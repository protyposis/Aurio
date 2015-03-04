/**
 * Simple proxy layer to read audio streams through FFmpeg.
 *
 *
 * This program contains code excerpts from:
 *
 ** decoding_encoding.c Copyright (c) 2001 Fabrice Bellard
 ** https://gitorious.org/ffmpeg/ffmpeg/source/07d508e4f55f6045b83df3346448b149faab5d7d:doc/examples/decoding_encoding.c
 **
 ** demuxing_decoding.c Copyright (c) 2012 Stefano Sabatini
 ** https://gitorious.org/ffmpeg/ffmpeg/source/07d508e4f55f6045b83df3346448b149faab5d7d:doc/examples/demuxing_decoding.c
 **
 ** Permission is hereby granted, free of charge, to any person obtaining a copy
 ** of this software and associated documentation files (the "Software"), to deal
 ** in the Software without restriction, including without limitation the rights
 ** to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 ** copies of the Software, and to permit persons to whom the Software is
 ** furnished to do so, subject to the following conditions:
 **
 ** The above copyright notice and this permission notice shall be included in
 ** all copies or substantial portions of the Software.
 */

/* Compatibility settings for the MSVC compiler */
#ifdef _MSC_VER
	#define inline __inline // support for "inline" http://stackoverflow.com/a/24435157
	#define snprintf _snprintf // support for "snprintf" http://stackoverflow.com/questions/2915672
	#define _CRT_SECURE_NO_WARNINGS // disable _snprintf compile warning
#endif

// System includes
#include <stdio.h>

// FFmpeg includes
#include "libavformat\avformat.h"
#include "libavutil\timestamp.h"
#include "libswresample\swresample.h"
#include "libavutil\opt.h"

// FFmpeg libs
#pragma comment(lib, "avformat.lib")
#pragma comment(lib, "avcodec.lib")
#pragma comment(lib, "avutil.lib")
#pragma comment(lib, "swresample.lib")

#define EXPORT __declspec(dllexport)
#define DEBUG 1



// structs
/* 
 * This struct holds all data necessary to manage an "instance" of a decoder,
 * and most importantly to run several decoders in parallel.
 */
typedef struct ProxyInstance {
	AVFormatContext		*fmt_ctx;
	AVStream			*audio_stream;
	AVCodecContext		*audio_codec_ctx;
	AVPacket			pkt;
	AVFrame				*frame;
	SwrContext			*swr;
	int					output_buffer_size;
	uint8_t				*output_buffer;

	struct {
		struct {
			int					sample_rate;
			int					sample_size; // bytes per sample (2 bytes for 16 bit int, 4 bytes for 32 bit float)
			int					channels;
		}					format;
		int64_t				length;
		int					frame_size;
	}					output;
} ProxyInstance;

// function definitions
EXPORT ProxyInstance *stream_open(char *filename);
EXPORT void *stream_get_output_config(ProxyInstance *pi);
EXPORT int stream_read_frame_any(ProxyInstance *pi, int *got_audio_frame);
EXPORT int stream_read_frame(ProxyInstance *pi);
EXPORT void stream_close(ProxyInstance *pi);

static void pi_init(ProxyInstance **pi);
static void pi_free(ProxyInstance **pi);

static void info(AVFormatContext *fmt_ctx);
static int open_audio_codec_context(AVFormatContext *fmt_ctx);
static int decode_audio_packet(ProxyInstance *pi, int *got_audio_frame, int cached);
static int convert_samples(ProxyInstance *pi);
static int determine_target_format(AVCodecContext *audio_codec_ctx);

int main(int argc, char *argv[])
{
	ProxyInstance *pi;

	if (argc < 2) {
		fprintf(stderr, "No source file specified\n");
		exit(1);
	}

	pi = stream_open(argv[1]);

	while (stream_read_frame(pi) >= 0) {
		// frame decoded
	}

	stream_close(pi);

	return 0;
}

ProxyInstance *stream_open(char *filename)
{
	ProxyInstance *pi;
	int ret;

	pi_init(&pi);

	av_register_all();

	if (avformat_open_input(&pi->fmt_ctx, filename, NULL, NULL) < 0) {
		fprintf(stderr, "Could not open source file %s\n", filename);
		exit(1);
	}

	if (avformat_find_stream_info(pi->fmt_ctx, NULL) < 0) {
		fprintf(stderr, "Could not find stream information\n");
		exit(1);
	}

	//av_dump_format(pi->fmt_ctx, 0, filename, 0);

	//info(pi->fmt_ctx);

	// open audio stream
	if ((ret = open_audio_codec_context(pi->fmt_ctx)) < 0) {
		fprintf(stderr, "Cannot find audio stream\n");
		exit(1);
	}

	pi->audio_stream = pi->fmt_ctx->streams[ret];
	pi->audio_codec_ctx = pi->audio_stream->codec;

	/* initialize sample format converter */
	// http://stackoverflow.com/a/15372417
	pi->swr = swr_alloc();
	if (!pi->audio_codec_ctx->channel_layout) {
		// when no channel layout is set, set default layout
		pi->audio_codec_ctx->channel_layout = av_get_default_channel_layout(pi->audio_codec_ctx->channels);
	}
	av_opt_set_int(pi->swr, "in_channel_layout", pi->audio_codec_ctx->channel_layout, 0);
	av_opt_set_int(pi->swr, "out_channel_layout", pi->audio_codec_ctx->channel_layout, 0);
	av_opt_set_int(pi->swr, "in_sample_rate", pi->audio_codec_ctx->sample_rate, 0);
	av_opt_set_int(pi->swr, "out_sample_rate", pi->audio_codec_ctx->sample_rate, 0);
	av_opt_set_sample_fmt(pi->swr, "in_sample_fmt", pi->audio_codec_ctx->sample_fmt, 0);
	av_opt_set_sample_fmt(pi->swr, "out_sample_fmt", determine_target_format(pi->audio_codec_ctx), 0);
	swr_init(pi->swr);



	/* initialize packet, set data to NULL, let the demuxer fill it */
	av_init_packet(&pi->pkt);
	pi->pkt.data = NULL;
	pi->pkt.size = 0;

	pi->frame = av_frame_alloc();

	/* set output properties */

	pi->output.format.sample_rate = pi->audio_codec_ctx->sample_rate;
	pi->output.format.sample_size = av_get_bytes_per_sample(determine_target_format(pi->audio_codec_ctx));
	pi->output.format.channels = pi->audio_codec_ctx->channels;

	if (DEBUG) {
		printf("output.format: %d sample_rate, %d sample_size, %d channels\n",
			pi->output.format.sample_rate,
			pi->output.format.sample_size,
			pi->output.format.channels);
	}

	/*
	 * TODO To get the frame size, read the first frame, take the size, and seek back to the start.
	 * This only works under the assumption that 
	 *  1. the frame size stays constant over time (are there codecs with variable sized frames?)
	 *  2. the first frame is always of "full" size
	 *  3. only the last frame can be smaller
	 * Alternatively, the frame size could be announced through a callback after reading the first
	 * frame, but this still requires an intermediate buffer. The best case would be to let the
	 * program that calls this library manage the buffer.
	 * 
	 * For now, a frame size of 1 second should be big enough to fit all occurring frame sizes (frame
	 * sizes were always smaller during tests).
	 */
	pi->output.length = (long)(av_q2d(pi->audio_stream->time_base) * 
		pi->audio_stream->duration * pi->output.format.sample_rate);
	pi->output.frame_size = pi->output.format.sample_rate; // 1 sec default frame size

	if (DEBUG) {
		printf("output: %lld length, %d frame_size\n", pi->output.length, pi->output.frame_size);
	}

	return pi;
}

void *stream_get_output_config(ProxyInstance *pi)
{
	return &pi->output;
}

/*
 * Reads the next frame in the stream.
 */
int stream_read_frame_any(ProxyInstance *pi, int *got_audio_frame)
{
	int ret;
	int cached = 0;

	/* 
	 * TODO to avoid possible memory leaks, open packets might have to be freed
	 * before EOF is returned.
	 */

	// if packet is emtpy, read new packet from stream
	if (pi->pkt.size == 0) {
		if ((ret = av_read_frame(pi->fmt_ctx, &pi->pkt)) < 0) {
			// probably EOF, check for cached frames (e.g. SHN)
			pi->pkt.data = NULL;
			pi->pkt.size = 0;
			cached = 1;
		}
	}

	ret = decode_audio_packet(pi, got_audio_frame, cached);
	
	if (ret < 0) {
		return -1; // decoding failed, signal EOF
	}
	else if (cached && !*got_audio_frame) {
		return -1; // signal the caller EOF
	}

	pi->pkt.data += ret;
	pi->pkt.size -= ret;

	if (convert_samples(pi) < 0) {
		return -1; // conversion failed, signal EOF
	}

	// free packet if all content has been read
	if (pi->pkt.size == 0) {
		av_free_packet(&pi->pkt);
	}

	/* 
	 * Return the number of samples per channel read, to keep API consistent.
	 * All "sizes" in the API are in samples, none in bytes.
	 */
	return ret / pi->output.format.channels / pi->output.format.sample_size; 
}

/*
 * Read the next audio frame, skipping other frame types in between.
 */
int stream_read_frame(ProxyInstance *pi) {
	int ret;
	int got_audio_frame;
	
	while (1) {
		ret = stream_read_frame_any(pi, &got_audio_frame);
		if (ret < 0 || got_audio_frame) {
			return ret;
		}
	}
}

void stream_seek(ProxyInstance *pi, int target)
{
	// TODO implement
}

void stream_close(ProxyInstance *pi)
{
	pi_free(&pi);
}

/*
* Initialize an instance data object to manage the decoding of audio.
*/
static void pi_init(ProxyInstance **pi) {
	ProxyInstance *_pi;
	*pi = _pi = malloc(sizeof(ProxyInstance));

	_pi->fmt_ctx = NULL;
	_pi->audio_stream = NULL;
	_pi->audio_codec_ctx = NULL;
	_pi->frame = NULL;
	_pi->swr = NULL;
	_pi->output_buffer_size = 0;
	_pi->output_buffer = NULL;
}

/*
* Destroy an instance data object.
*/
static void pi_free(ProxyInstance **pi) {
	ProxyInstance *_pi = *pi;

	/* close & free FFmpeg stuff */
	av_free_packet(&_pi->pkt);
	av_frame_free(&_pi->frame);
	free(_pi->output_buffer);
	swr_free(&_pi->swr);
	avcodec_close(_pi->audio_codec_ctx);
	avformat_close_input(&_pi->fmt_ctx);

	/* free instance data */
	free(_pi);
}

static void info(AVFormatContext *fmt_ctx)
{
	AVStream *stream;
	AVCodecContext *codec_ctx;
	AVCodec *codec;

	printf("%d stream(s) found:\n", fmt_ctx->nb_streams);

	for (unsigned int i = 0; i < fmt_ctx->nb_streams; i++) {
		AVDictionary *opts = NULL;

		stream = fmt_ctx->streams[i];
		codec_ctx = fmt_ctx->streams[i]->codec;

		// print stream info
		// http://ffmpeg.org/doxygen/trunk/structAVStream.html
		printf("STREAM INDEX %d\n", stream->index);
		printf("  frame rate: .......... %d/%d (real base frame rate)\n", stream->r_frame_rate.num, stream->r_frame_rate.den);
		printf("  time base: ........... %d/%d\n", stream->time_base.num, stream->time_base.den);
		printf("  start time: .......... %lld\n", stream->start_time);
		printf("  duration: ............ %lld\n", stream->duration);
		printf("  number of frames: .... %lld\n", stream->nb_frames);
		printf("  sample aspect ratio: . %d:%d\n", stream->sample_aspect_ratio.num, stream->sample_aspect_ratio.den);
		printf("  calculated length: ... %s\n", av_ts2timestr(stream->duration, &stream->time_base));

		// print codec context info
		// http://ffmpeg.org/doxygen/trunk/structAVCodecContext.html
		printf("  CODEC CONTEXT:\n");
		printf("    average bit rate: .. %d\n", codec_ctx->bit_rate);
		printf("    time base: ......... %d/%d\n", codec_ctx->time_base.num, codec_ctx->time_base.den);
		printf("    width: ............. %d\n", codec_ctx->width);
		printf("    height: ............ %d\n", codec_ctx->height);
		printf("    gop size: .......... %d\n", codec_ctx->gop_size);
		printf("    pixel format: ...... %d\n", codec_ctx->pix_fmt);
		printf("    sample rate: ....... %d\n", codec_ctx->sample_rate);
		printf("    channels: .......... %d\n", codec_ctx->channels);
		printf("    sample format: ..... %d\n", codec_ctx->sample_fmt);
		printf("    codec type: ........ %d\n", codec_ctx->codec_type);
		printf("    codec id: .......... %d\n", codec_ctx->codec_id);
		printf("    codec tag: ......... %c%c%c%c (fourcc)\n", codec_ctx->codec_tag, codec_ctx->codec_tag >> 8, codec_ctx->codec_tag >> 16, codec_ctx->codec_tag >> 24);
		printf("    sample aspect ratio: %d:%d\n", codec_ctx->sample_aspect_ratio.num, codec_ctx->sample_aspect_ratio.den);

		codec = avcodec_find_decoder(codec_ctx->codec_id);
		if (!codec) {
			printf("cannot find decoder for CODEC_ID %d\n", codec_ctx->codec_id);
		}
		//c = avcodec_alloc_context();
		if (avcodec_open2(codec_ctx, codec, &opts) < 0)	{
			printf("cannot open codec %d\n", codec_ctx->codec_id);
		}

		// print codec info
		// http://ffmpeg.org/doxygen/trunk/structAVCodec.html
		printf("  CODEC:\n");
		printf("    name: .............. %s\n", codec->name);
		printf("    name (long): ....... %s\n", codec->long_name);
		printf("    type: .............. %d\n", codec->type);
		printf("    id:   .............. %d\n", codec->id);

		printf("stream %d: %s - %s [%d/%d]\n", i, av_get_media_type_string(codec_ctx->codec_type), codec_ctx->codec_name, codec_ctx->codec_type, codec_ctx->codec_id);
		printf("\n");
	}
}

static int open_audio_codec_context(AVFormatContext *fmt_ctx)
{
	int stream_idx;
	AVStream *stream = NULL;
	AVCodecContext *codec_ctx = NULL;
	AVCodec *codec = NULL;
	AVDictionary *opts = NULL;

	/* Find stream of given type */
	stream_idx = av_find_best_stream(fmt_ctx, AVMEDIA_TYPE_AUDIO, -1, -1, NULL, 0);

	if (stream_idx < 0) {
		fprintf(stderr, "Could not find stream\n");
		return -1;
	}
	else {
		stream = fmt_ctx->streams[stream_idx];

		/* find decoder for the stream */
		codec_ctx = stream->codec;
		codec = avcodec_find_decoder(codec_ctx->codec_id);
		if (!codec) {
			fprintf(stderr, "Failed to find codec\n");
			return -2;
		}

		/* Init the decoder */
		if (avcodec_open2(codec_ctx, codec, &opts) < 0) {
			fprintf(stderr, "Failed to open codec\n");
			return -3;
		}

		if (DEBUG) {
			printf("sampleformat: %s, planar: %d, channels: %d, raw bitdepth: %d, bitdepth: %d\n",
				av_get_sample_fmt_name(codec_ctx->sample_fmt),
				av_sample_fmt_is_planar(codec_ctx->sample_fmt),
				codec_ctx->channels,
				codec_ctx->bits_per_raw_sample,
				av_get_bytes_per_sample(codec_ctx->sample_fmt) * 8);
		}
	}

	return stream_idx;
}

static int audio_frame_count = 0;
/*
 * Decodes an audio frame and returns the number of bytes consumed from the input packet,
 * or a negative error code (it is basically the result of avcodec_decode_audio4).
 */
static int decode_audio_packet(ProxyInstance *pi, int *got_audio_frame, int cached)
{
	int ret = 0;
	int decoded = pi->pkt.size; // to skip non-target stream packets, return the full packet size

	*got_audio_frame = 0;

	if (pi->pkt.stream_index == pi->audio_stream->index) {
		/* decode audio frame */
		ret = avcodec_decode_audio4(pi->audio_codec_ctx, pi->frame, got_audio_frame, &pi->pkt);
		if (ret < 0) {
			fprintf(stderr, "Error decoding audio frame (%s)\n", av_err2str(ret));
			return ret;
		}
		/* Some audio decoders decode only part of the packet, and have to be
		* called again with the remainder of the packet data.
		* Sample: fate-suite/lossless-audio/luckynight-partial.shn
		* Also, some decoders might over-read the packet. */
		decoded = FFMIN(ret, pi->pkt.size);

		if (*got_audio_frame && DEBUG) {
			printf("packet dts:%s pts:%s duration:%s\n",
				av_ts2timestr(pi->pkt.dts, &pi->audio_stream->time_base),
				av_ts2timestr(pi->pkt.pts, &pi->audio_stream->time_base),
				av_ts2timestr(pi->pkt.duration, &pi->audio_stream->time_base));

			printf("audio_frame%s n:%d nb_samples:%d pts:%s\n",
				cached ? "(cached)" : "",
				audio_frame_count++, pi->frame->nb_samples,
				av_ts2timestr(pi->frame->pts, &pi->audio_stream->time_base));
		}
	}

	return decoded;
}

static int convert_samples(ProxyInstance *pi) {
	/* prepare/update sample format conversion buffer */
	int output_buffer_size_needed = pi->frame->nb_samples * pi->frame->channels * av_get_bytes_per_sample(pi->audio_codec_ctx->sample_fmt);
	if (pi->output_buffer_size < output_buffer_size_needed) {
		if(DEBUG) printf("init swr output buffer size %d\n", output_buffer_size_needed);
		if (pi->output_buffer != NULL) {
			free(pi->output_buffer);
		}
		pi->output_buffer = malloc(output_buffer_size_needed);
		pi->output_buffer_size = output_buffer_size_needed;
	}

	/* convert samples to target format */
	int ret = swr_convert(pi->swr, &pi->output_buffer, pi->frame->nb_samples, pi->frame->extended_data, pi->frame->nb_samples);
	if (ret < 0) {
		fprintf(stderr, "Could not convert input samples\n");
	}
	else if (ret != pi->frame->nb_samples) {
		fprintf(stderr, "Output sample count != input sample count (%d != %d)\n", ret, pi->frame->nb_samples);
	}

	return ret; // if >= 0, the number of samples converted
}

/* 
 * Determines the always interleaved sample format to be output from this decoding layer.
 */
static int determine_target_format(AVCodecContext *audio_codec_ctx)
{
	int raw_bitdepth = audio_codec_ctx->bits_per_raw_sample;
	int bitdepth = av_get_bytes_per_sample(audio_codec_ctx->sample_fmt) * 8;

	if (raw_bitdepth == 16) {
		return AV_SAMPLE_FMT_S16;
	}
	else if (raw_bitdepth > 16) {
		return AV_SAMPLE_FMT_FLT;
	}
	else if (bitdepth == 16) {
		return AV_SAMPLE_FMT_S16;
	}
	else if (bitdepth > 16) {
		return AV_SAMPLE_FMT_FLT;
	}
	else {
		fprintf(stderr, "unsupported sample format %d/%d/%s, fallback to default\n", 
			raw_bitdepth, bitdepth, 
			av_get_sample_fmt_name(audio_codec_ctx->sample_fmt));
	}

	// default format
	return AV_SAMPLE_FMT_FLT;
}
