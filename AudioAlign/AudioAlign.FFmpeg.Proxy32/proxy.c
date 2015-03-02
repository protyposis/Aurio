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
#include <libavformat\avformat.h>
#include <libavutil\timestamp.h>

// FFmpeg libs
#pragma comment(lib, "avformat.lib")
#pragma comment(lib, "avcodec.lib")
#pragma comment(lib, "avutil.lib")

// function definitions
static void info(AVFormatContext *fmt_ctx);
static int open_codec_context(AVFormatContext *fmt_ctx, enum AVMediaType type);
static int decode_audio_packet(int audio_stream_idx, AVPacket pkt, AVCodecContext *audio_codec_ctx, AVFrame *frame, int *got_frame, int cached);

int main(int argc, char *argv[])
{
	char *src_filename;
	AVFormatContext *fmt_ctx = NULL;
	int audio_stream_idx;
	AVStream *audio_stream = NULL;
	AVCodecContext *audio_codec_ctx = NULL;
	AVPacket pkt;
	AVFrame *frame = NULL;
	int ret;
	int got_frame;

	if (argc < 2) {
		fprintf(stderr, "No source file specified\n");
		exit(1);
	}
	else {
		src_filename = argv[1];
	}

	av_register_all();
	
	if (avformat_open_input(&fmt_ctx, src_filename, NULL, NULL) < 0) {
		fprintf(stderr, "Could not open source file %s\n", src_filename);
		exit(1);
	}

	if (avformat_find_stream_info(fmt_ctx, NULL) < 0) {
		fprintf(stderr, "Could not find stream information\n");
		exit(1);
	}

	//av_dump_format(fmt_ctx, 0, src_filename, 0);

	//info(fmt_ctx);

	// open audio stream
	if ((audio_stream_idx = open_codec_context(fmt_ctx, AVMEDIA_TYPE_AUDIO)) < 0) {
		fprintf(stderr, "Cannot find audio stream\n");
		exit(1);
	}

	audio_stream = fmt_ctx->streams[audio_stream_idx];
	audio_codec_ctx = audio_stream->codec;

	/* initialize packet, set data to NULL, let the demuxer fill it */
	av_init_packet(&pkt);
	pkt.data = NULL;
	pkt.size = 0;

	frame = av_frame_alloc();


	/* read frames from the file */
	/* You should use libswresample or libavfilter to convert the frame
	 * to packed data. */
	while (av_read_frame(fmt_ctx, &pkt) >= 0) {
		AVPacket orig_pkt = pkt;
		do {
			ret = decode_audio_packet(audio_stream_idx, pkt, audio_codec_ctx, frame, &got_frame, 0);
			if (ret < 0)
				break;
			pkt.data += ret;
			pkt.size -= ret;
		} while (pkt.size > 0);
		av_free_packet(&orig_pkt);
	}

	/* flush cached frames (e.g. in SHN) */
	pkt.data = NULL;
	pkt.size = 0;
	do {
		decode_audio_packet(audio_stream_idx, pkt, audio_codec_ctx, frame, &got_frame, 1);
	} while (got_frame);


	/* close open FFmpeg stuff */
	av_free_packet(&pkt);
	av_frame_free(&frame);
	avcodec_close(audio_codec_ctx);
	avformat_close_input(&fmt_ctx);

	return 0;
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

static int open_codec_context(AVFormatContext *fmt_ctx, enum AVMediaType type)
{
	int stream_idx;
	AVStream *stream = NULL;
	AVCodecContext *codec_ctx = NULL;
	AVCodec *codec = NULL;
	AVDictionary *opts = NULL;

	/* Find stream of given type */
	stream_idx = av_find_best_stream(fmt_ctx, AVMEDIA_TYPE_AUDIO, -1, -1, NULL, 0);

	if (stream_idx < 0) {
		fprintf(stderr, "Could not find stream\n", av_get_media_type_string(type));
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
	}

	return stream_idx;
}

static int audio_frame_count = 0;
static int decode_audio_packet(int audio_stream_idx, AVPacket pkt, AVCodecContext *audio_codec_ctx, AVFrame *frame, int *got_frame, int cached)
{
	int ret = 0;
	int decoded = pkt.size; // to skip non-target stream packets, return the full packet size

	*got_frame = 0;

	if (pkt.stream_index == audio_stream_idx) {
		/* decode audio frame */
		ret = avcodec_decode_audio4(audio_codec_ctx, frame, got_frame, &pkt);
		if (ret < 0) {
			fprintf(stderr, "Error decoding audio frame (%s)\n", av_err2str(ret));
			return ret;
		}
		/* Some audio decoders decode only part of the packet, and have to be
		* called again with the remainder of the packet data.
		* Sample: fate-suite/lossless-audio/luckynight-partial.shn
		* Also, some decoders might over-read the packet. */
		decoded = FFMIN(ret, pkt.size);

		if (*got_frame) {
			size_t unpadded_linesize = frame->nb_samples * av_get_bytes_per_sample(frame->format);
			printf("audio_frame%s n:%d nb_samples:%d pts:%s\n",
				cached ? "(cached)" : "",
				audio_frame_count++, frame->nb_samples,
				av_ts2timestr(frame->pts, &audio_codec_ctx->time_base));

		}
	}

	return decoded;
}