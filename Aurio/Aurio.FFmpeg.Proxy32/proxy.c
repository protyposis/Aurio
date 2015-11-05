// 
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2015  Mario Guggenberger <mg@protyposis.net>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

/* Compatibility settings for the MSVC compiler */
#ifdef _MSC_VER
	#define inline __inline // support for "inline" http://stackoverflow.com/a/24435157
	#if _MSC_VER < 1900 // snfprint support added in VS2015 http://stackoverflow.com/a/27754829
		#define snprintf _snprintf // support for "snprintf" http://stackoverflow.com/questions/2915672
	#endif
	#define _CRT_SECURE_NO_WARNINGS // disable _snprintf compile warning, disable fopen compile error
#endif

// System includes
#include <stdio.h>

// FFmpeg includes
#include "libavformat\avformat.h"
#include "libavutil\timestamp.h"
#include "libswresample\swresample.h"
#include "libavutil\opt.h"
#include "libswscale\swscale.h"

// FFmpeg libs
#pragma comment(lib, "avformat.lib")
#pragma comment(lib, "avcodec.lib")
#pragma comment(lib, "avutil.lib")
#pragma comment(lib, "swresample.lib")
#pragma comment(lib, "swscale.lib")

#define EXPORT __declspec(dllexport)
#define DEBUG 1


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


// structs
/* 
 * This struct holds all data necessary to manage an "instance" of a decoder,
 * and most importantly to run several decoders in parallel.
 */
typedef struct ProxyInstance {
	int					mode;
	AVFormatContext		*fmt_ctx;
	AVStream			*audio_stream;
	AVStream			*video_stream;
	AVCodecContext		*audio_codec_ctx;
	AVCodecContext		*video_codec_ctx;
	AVPacket			pkt;
	AVFrame				*frame;
	SwrContext			*swr;
	struct SwsContext	*sws;
	AVPicture			video_picture;
	int					output_buffer_size;
	uint8_t				*output_buffer;
	int64_t				frame_pts;

	struct {
		struct {
			int					sample_rate;
			int					sample_size; // bytes per sample (2 bytes for 16 bit int, 4 bytes for 32 bit float)
			int					channels;
		}					format;
		int64_t				length;
		int					frame_size;
	}					audio_output;

	struct {
		struct {
			int					width;
			int					height;
			double				frame_rate;
			double				aspect_ratio;
		}					format;
		int64_t				length;
		int					frame_size;
	}					video_output;
} ProxyInstance;

#define MODE_NONE  0x00
#define MODE_AUDIO 0x01
#define MODE_VIDEO 0x02

// function definitions
EXPORT ProxyInstance *stream_open_file(int mode, char *filename);
EXPORT ProxyInstance *stream_open_bufferedio(int mode, void *opaque, int(*read_packet)(void *opaque, uint8_t *buf, int buf_size), int64_t(*seek)(void *opaque, int64_t offset, int whence));
ProxyInstance *stream_open(ProxyInstance *pi);
EXPORT void *stream_get_output_config(ProxyInstance *pi);
EXPORT int stream_read_frame_any(ProxyInstance *pi, int *got_frame, int *frame_type);
EXPORT int stream_read_frame(ProxyInstance *pi, int64_t *timestamp, uint8_t *output_buffer, int output_buffer_size, int *frame_type);
EXPORT void stream_seek(ProxyInstance *pi, int64_t timestamp);
EXPORT void stream_close(ProxyInstance *pi);

static void pi_init(ProxyInstance **pi);
static void pi_free(ProxyInstance **pi);

static void info(AVFormatContext *fmt_ctx);
static int open_codec_context(AVFormatContext *fmt_ctx, int type);
static int decode_audio_packet(ProxyInstance *pi, int *got_audio_frame, int cached);
static int decode_video_packet(ProxyInstance *pi, int *got_video_frame, int cached);
static int convert_audio_samples(ProxyInstance *pi);
static int convert_video_frame(ProxyInstance *pi);
static int determine_target_format(AVCodecContext *audio_codec_ctx);
static inline int64_t pts_to_samples(ProxyInstance *pi, AVRational time_base, int64_t time);
static inline int64_t samples_to_pts(ProxyInstance *pi, AVRational time_base, int64_t time);

// THESE FUNCTIONS ARE ONLY FOR STANDALONE DEBUG PURPOSES
FILE *file_open(const char *filename) {
	return fopen(filename, "rb");
}

void file_rewind(FILE *f) {
	rewind(f);
}

int file_close(FILE *f) {
	return fclose(f);
}

int file_read_packet(FILE* f, uint8_t *buf, int buf_size) {
	return fread(buf, 1, buf_size, f);
}

int64_t file_seek(FILE* f, int64_t offset, int whence) {
	if (whence == AVSEEK_SIZE) {
		long current_pos = ftell(f);		// temporarily save current position
		fseek(f, 0, SEEK_END);				// seek to end
		long file_size = ftell(f);			// end position == file size
		fseek(f, current_pos, SEEK_SET);	// return to original position
		return file_size;
	}
	return fseek(f, (long)offset, whence);
}
//////////////////////////////////////////////////////////

int main(int argc, char *argv[])
{
	ProxyInstance *pi;
	int64_t timestamp;
	int ret;
	uint8_t *output_buffer;
	int audio_output_buffer_size = 0, video_output_buffer_size = 0, output_buffer_size;
	int frame_type;
	const int stream_mode = 1; // 0 =  file, 1 = buffered stream IO
	FILE *f = NULL; // used for buffered stream IO
	int mode = MODE_VIDEO;

	if (argc < 2) {
		fprintf(stderr, "No source file specified\n");
		exit(1);
	}

	if (stream_mode) { // buffered stream IO
		f = file_open(argv[1]);
		if (!f) {
			fprintf(stderr, "input file not found: %s\n", argv[1]);
			exit(1);
		}
		pi = stream_open_bufferedio(mode, f, file_read_packet, file_seek);
	}
	else { // file IO
		pi = stream_open_file(mode, argv[1]);
	}

	//info(pi->fmt_ctx);

	if (mode & MODE_AUDIO) {
		printf("audio length: %lld, frame size: %d\n", pi->audio_output.length, pi->audio_output.frame_size);
		printf("audio format (samplerate/samplesize/channels): %d/%d/%d\n",
			pi->audio_output.format.sample_rate, pi->audio_output.format.sample_size, pi->audio_output.format.channels);

		audio_output_buffer_size = pi->audio_output.frame_size * pi->audio_output.format.channels * pi->audio_output.format.sample_size;
	}
	if (mode & MODE_VIDEO) {
		printf("video length: %lld, frame size: %d\n", pi->video_output.length, pi->video_output.frame_size);
		printf("video format (width/height/fps/aspect): %d/%d/%f/%f\n",
			pi->video_output.format.width, pi->video_output.format.height, pi->video_output.format.frame_rate, pi->video_output.format.aspect_ratio);

		video_output_buffer_size = pi->video_output.frame_size;
	}
	output_buffer_size = max(audio_output_buffer_size, video_output_buffer_size);
	output_buffer = malloc(output_buffer_size);

	// read full stream
	int64_t count1 = 0, last_ts1;
	while ((ret = stream_read_frame(pi, &timestamp, output_buffer, output_buffer_size, &frame_type)) >= 0) {
		printf("read %d @ %lld type %d\n", ret, timestamp, frame_type);
		count1++;
		last_ts1 = timestamp;
	}

	// seek back to start
	stream_seek(pi, 0);

	// read again (output should be the same as above)
	int64_t count2 = 0, last_ts2;
	while ((ret = stream_read_frame(pi, &timestamp, output_buffer, output_buffer_size, &frame_type)) >= 0) {
		printf("read %d @ %lld type %d\n", ret, timestamp, frame_type);
		count2++;
		last_ts2 = timestamp;
	}

	printf("read1 count: %lld, timestamp: %lld\n", count1, last_ts1);
	printf("read2 count: %lld, timestamp: %lld\n", count2, last_ts2);

	free(output_buffer);

	stream_close(pi);

	if (stream_mode) {
		file_close(f);
	}

	return 0;
}

/*
 * Opens a stream from a file specified by filename.
 */
ProxyInstance *stream_open_file(int mode, char *filename)
{
	ProxyInstance *pi;

	pi_init(&pi);
	pi->mode = mode;

	av_register_all();

	if (avformat_open_input(&pi->fmt_ctx, filename, NULL, NULL) < 0) {
		fprintf(stderr, "Could not open source file %s\n", filename);
		exit(1);
	}

	return stream_open(pi);
}

/*
 * Opens a buffered I/O stream through data reading callbacks, allowing for arbitrary data sources (e.g. online streams, custom file input streams).
 */
ProxyInstance *stream_open_bufferedio(int mode,
	// User-specific data that is returned with each callback (e.g. an instance pointer, a stream id, or the source stream object). Optional, can be NULL.
	void *opaque, 
	// Callback to read a data packet of given length.
	int(*read_packet)(void *opaque, uint8_t *buf, int buf_size), 
	// Callback for a seek operation. Optional, can be NULL.
	// whence: SEEK_SET/0, SEEK_CUR/1, SEEK_END/2, AVSEEK_SIZE/0x10000 (optional, return -1 of not supported), AVSEEK_FORCE/0x20000 (ored into whence, can be ignored)
	int64_t(*seek)(void *opaque, int64_t offset, int whence))
{
	ProxyInstance *pi;
	const int buffer_size = 32 * 1024;
	char *buffer;
	AVIOContext *io_ctx;
	int ret;

	pi_init(&pi);
	pi->mode = mode;

	av_register_all();

	// Allocate IO buffer for the AVIOContext. 
	// Must later be freed by av_free() from AVIOContext.buffer (which could be the same or a replacement buffer).
	buffer = av_malloc(buffer_size + FF_INPUT_BUFFER_PADDING_SIZE);

	// Allocate the AVIOContext. Must later be freed by av_free().
	io_ctx = avio_alloc_context(buffer, buffer_size, 0 /* not writeable */, opaque, read_packet, NULL /* no write_packet needed */, seek);

	// Allocate and configure AVFormatContext. Must later bee freed by avformat_close_input().
	pi->fmt_ctx = avformat_alloc_context();
	pi->fmt_ctx->pb = io_ctx;

	// NOTE format does not need to be probed manually, FFmpeg does the probing itself and does not crash anymore

	if ((ret = avformat_open_input(&pi->fmt_ctx, NULL, NULL, NULL)) < 0) {
		fprintf(stderr, "Could not open source stream: %s\n", av_err2str(ret));
		exit(1);
	}

	// NOTE AVFMT_FLAG_CUSTOM_IO is automatically set by avformat_open_input, can be checked when closing the stream to free allocated resources

	return stream_open(pi);
}

/*
 * Opens a stream from an initialized AVFormatContext. The AVFormatContext needs to be 
 * initialized separately, to allow for filename and buffered IO contexts.
 */
ProxyInstance *stream_open(ProxyInstance *pi)
{
	int ret;

	if (pi->mode == MODE_NONE) {
		fprintf(stderr, "no mode specified");
		exit(1);
	}

	if (pi->fmt_ctx == NULL) {
		fprintf(stderr, "AVFormatContext missing / not initialized");
		exit(1);
	}

	if (avformat_find_stream_info(pi->fmt_ctx, NULL) < 0) {
		fprintf(stderr, "Could not find stream information\n");
		exit(1);
	}

	//av_dump_format(pi->fmt_ctx, 0, filename, 0);

	//info(pi->fmt_ctx);

	if (pi->mode & MODE_AUDIO) {
		// open audio stream
		if ((ret = open_codec_context(pi->fmt_ctx, AVMEDIA_TYPE_AUDIO)) < 0) {
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
	

		/* set output properties */

		pi->audio_output.format.sample_rate = pi->audio_codec_ctx->sample_rate;
		pi->audio_output.format.sample_size = av_get_bytes_per_sample(determine_target_format(pi->audio_codec_ctx));
		pi->audio_output.format.channels = pi->audio_codec_ctx->channels;

		if (DEBUG) {
			printf("audio_output.format: %d sample_rate, %d sample_size, %d channels\n",
				pi->audio_output.format.sample_rate,
				pi->audio_output.format.sample_size,
				pi->audio_output.format.channels);
		}

		pi->audio_output.length = pi->audio_stream->duration != AV_NOPTS_VALUE ?
			pts_to_samples(pi, pi->audio_stream->time_base, pi->audio_stream->duration) : AV_NOPTS_VALUE;

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
		pi->audio_output.frame_size = pi->audio_output.format.sample_rate; // 1 sec default frame size

		if (DEBUG) {
			printf("output: %lld length, %d frame_size\n", pi->audio_output.length, pi->audio_output.frame_size);
		}

		if (pi->audio_codec_ctx->codec->capabilities & CODEC_CAP_DELAY) {
			// When CODEC_CAP_DELAY is set, there is a delay between input and output of the decoder
			printf("warning: cap delay!\n");
		}
	}

	if (pi->mode & MODE_VIDEO) {
		// open audio stream
		if ((ret = open_codec_context(pi->fmt_ctx, AVMEDIA_TYPE_VIDEO)) < 0) {
			fprintf(stderr, "Cannot find video stream\n");
			exit(1);
		}

		pi->video_stream = pi->fmt_ctx->streams[ret];
		pi->video_codec_ctx = pi->video_stream->codec;

		/* Initialize video frame converter */
		// PIX_FMT_BGR24 format needed by C# for correct color interpretation (PixelFormat.Format24bppRgb)
		pi->sws = sws_getContext(pi->video_codec_ctx->width, pi->video_codec_ctx->height, pi->video_codec_ctx->pix_fmt, 
			pi->video_codec_ctx->width, pi->video_codec_ctx->height, PIX_FMT_BGR24, SWS_BICUBIC, NULL, NULL, NULL);
		if (pi->sws == NULL) {
			fprintf(stderr, "error creating swscontext\n");
			exit(1);
		}

		if (avpicture_alloc(&pi->video_picture, PIX_FMT_RGB24, pi->video_codec_ctx->width, pi->video_codec_ctx->height) != 0) {
			fprintf(stderr, "error allocating AVPicture\n");
			exit(1);
		}

		/* set output properties */

		pi->video_output.format.width = pi->video_codec_ctx->width;
		pi->video_output.format.height = pi->video_codec_ctx->height;
		pi->video_output.format.frame_rate = av_q2d(pi->video_codec_ctx->framerate);
		pi->video_output.format.aspect_ratio = av_q2d(pi->video_codec_ctx->sample_aspect_ratio);

		if (DEBUG) {
			printf("video_output.format: %d width, %d height, %f frame_rate, %f aspect_ratio\n",
				pi->video_output.format.width,
				pi->video_output.format.height,
				pi->video_output.format.frame_rate,
				pi->video_output.format.aspect_ratio);
		}

		pi->video_output.length = pi->video_stream->duration != AV_NOPTS_VALUE ?
			pts_to_samples(pi, pi->video_stream->time_base, pi->video_stream->duration) : AV_NOPTS_VALUE;

		pi->video_output.frame_size = pi->video_output.format.width * pi->video_output.format.height * 4; // TODO determine real size

		if (DEBUG) {
			printf("output: %lld length, %d frame_size\n", pi->video_output.length, pi->video_output.frame_size);
		}

		if (pi->video_codec_ctx->codec->capabilities & CODEC_CAP_DELAY) {
			// When CODEC_CAP_DELAY is set, there is a delay between input and output of the decoder
			printf("warning: cap delay!\n");
		}
	}

	/* initialize packet, set data to NULL, let the demuxer fill it */
	av_init_packet(&pi->pkt);
	pi->pkt.data = NULL;
	pi->pkt.size = 0;

	pi->frame = av_frame_alloc();

	return pi;
}

void *stream_get_output_config(ProxyInstance *pi)
{
	return &pi->audio_output;
}

/*
 * Reads the next frame in the stream.
 */
int stream_read_frame_any(ProxyInstance *pi, int *got_frame, int *frame_type)
{
	int ret;
	int cached = 0;

	*got_frame = 0;
	*frame_type = MODE_NONE;

	// if packet is emtpy, read new packet from stream
	if (pi->pkt.size == 0) {
		if ((ret = av_read_frame(pi->fmt_ctx, &pi->pkt)) < 0) {
			// probably EOF, check for cached frames (e.g. SHN)
			pi->pkt.data = NULL;
			pi->pkt.size = 0;
			cached = 1;
		}
	}

	if (pi->mode & MODE_AUDIO) {
		ret = decode_audio_packet(pi, got_frame, cached);
	}
	if (*got_frame) {
		*frame_type = MODE_AUDIO;
	}
	else if (pi->mode & MODE_VIDEO) {
		ret = decode_video_packet(pi, got_frame, cached);
	}
	if (*got_frame) {
		*frame_type = MODE_VIDEO;
	}
	
	if (ret < 0) {
		av_free_packet(&pi->pkt);
		return -1; // decoding failed, signal EOF
	}
	else if (cached && !*got_frame) {
		av_free_packet(&pi->pkt);
		return -1; // signal the caller EOF
	}

	pi->pkt.data += ret;
	pi->pkt.size -= ret;

	if (*frame_type == MODE_AUDIO && convert_audio_samples(pi) < 0) {
		av_free_packet(&pi->pkt);
		return -1; // conversion failed, signal EOF
	}
	else if (*frame_type == MODE_VIDEO && convert_video_frame(pi) < 0) {
		av_free_packet(&pi->pkt);
		return -1; // conversion failed, signal EOF
	}

	// free packet if all content has been read
	if (pi->pkt.size == 0) {
		av_free_packet(&pi->pkt);
	}

	if (*got_frame) {
		pi->frame_pts = pi->pkt.pts;
	}

	/* 
	 * Return the number of samples per channel read, to keep API consistent.
	 * All "sizes" in the API are in samples, none in bytes.
	 */
	return pi->frame->nb_samples; // TODO return some video size as well (this only returns the audio samples)
}

/*
 * Read the next desired frame, skipping other frame types in between.
 */
int stream_read_frame(ProxyInstance *pi, int64_t *timestamp, uint8_t *output_buffer, int output_buffer_size, int *frame_type)
{
	int ret;
	int got_frame;

	*timestamp = -1;
	
	while (1) {
		pi->output_buffer = output_buffer;
		pi->output_buffer_size = output_buffer_size;
		ret = stream_read_frame_any(pi, &got_frame, frame_type);
		if (ret < 0 || got_frame) {
			if (*frame_type == MODE_AUDIO) {
				*timestamp = pi->pkt.pts != AV_NOPTS_VALUE ?
					pts_to_samples(pi, pi->audio_stream->time_base, pi->pkt.pts) : pi->pkt.pos;
			}
			else if (*frame_type == MODE_VIDEO) {
				*timestamp = pi->pkt.pts != AV_NOPTS_VALUE ?
					pts_to_samples(pi, pi->video_stream->time_base, pi->pkt.pts) : pi->pkt.pos;
			}
			return ret;
		}
	}
}

void stream_seek(ProxyInstance *pi, int64_t timestamp)
{
	// convert sample time to time_base time
	timestamp = samples_to_pts(pi, pi->audio_stream->time_base, timestamp);

	/*
	 * When seeking to a timestamp which is not exactly a frame PTS but 
	 * between two frame PTS' a and b, 
	 * thus PTS(a) < seek_timestamp < PTS(b), e.g.:
	 *
	 *   ...........|....................................|................
	 *              ^ PTS(a)     ^ seek_timestamp        ^ PTS(b)
	 * 
	 * then the position after the seek will be PTS(b). By specifying the
	 * flag AVSEEK_FLAG_BACKWARD, it will end up at PTS(a).
	 *
	 * This applies to both seek directions, backward and forward from the
	 * current position in the stream.
	 */

	// do seek
	av_seek_frame(pi->fmt_ctx, pi->audio_stream->index, timestamp, AVSEEK_FLAG_BACKWARD);
	
	// flush codec
	avcodec_flush_buffers(pi->audio_codec_ctx);

	// avcodec_flush_buffers invalidates the packet reference
	pi->pkt.data = NULL;
	pi->pkt.size = 0;
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
	_pi->video_stream = NULL;
	_pi->audio_codec_ctx = NULL;
	_pi->video_codec_ctx = NULL;
	_pi->frame = NULL;
	_pi->swr = NULL;
	_pi->sws = NULL;
	_pi->output_buffer_size = 0;
	_pi->output_buffer = NULL;
}

/*
* Destroy an instance data object.
*/
static void pi_free(ProxyInstance **pi) {
	ProxyInstance *_pi = *pi;

	/* close & free FFmpeg stuff */
	if ((_pi->fmt_ctx->flags & AVFMT_FLAG_CUSTOM_IO) != 0) {
		// buffered stream IO mode
		av_free(_pi->fmt_ctx->pb->buffer);
		av_free(_pi->fmt_ctx->pb);
	}
	sws_freeContext(_pi->sws);
	avpicture_free(&_pi->video_picture);
	av_free_packet(&_pi->pkt);
	av_frame_free(&_pi->frame);
	swr_free(&_pi->swr);
	avcodec_close(_pi->audio_codec_ctx);
	avcodec_close(_pi->video_codec_ctx);
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

static int open_codec_context(AVFormatContext *fmt_ctx, int type)
{
	int stream_idx;
	AVStream *stream = NULL;
	AVCodecContext *codec_ctx = NULL;
	AVCodec *codec = NULL;
	AVDictionary *opts = NULL;

	/* Find stream of given type */
	stream_idx = av_find_best_stream(fmt_ctx, type, -1, -1, NULL, 0);

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
			if (type == AVMEDIA_TYPE_AUDIO) {
				printf("audio sampleformat: %s, planar: %d, channels: %d, raw bitdepth: %d, bitdepth: %d\n",
					av_get_sample_fmt_name(codec_ctx->sample_fmt),
					av_sample_fmt_is_planar(codec_ctx->sample_fmt),
					codec_ctx->channels,
					codec_ctx->bits_per_raw_sample,
					av_get_bytes_per_sample(codec_ctx->sample_fmt) * 8);
			}
			else if (type == AVMEDIA_TYPE_VIDEO) {
				printf("video sampleformat: raw bitdepth: %d\n",
					codec_ctx->bits_per_raw_sample);
			}
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

static int video_frame_count = 0;
/*
* Decodes a video frame and returns the number of bytes consumed from the input packet,
* or a negative error code (it is basically the result of avcodec_decode_audio4).
*/
static int decode_video_packet(ProxyInstance *pi, int *got_video_frame, int cached)
{
	int ret = 0;
	int decoded = pi->pkt.size; // to skip non-target stream packets, return the full packet size

	*got_video_frame = 0;

	if (pi->pkt.stream_index == pi->video_stream->index) {
		/* decode audio frame */
		ret = avcodec_decode_video2(pi->video_codec_ctx, pi->frame, got_video_frame, &pi->pkt);
		if (ret < 0) {
			fprintf(stderr, "Error decoding video frame (%s)\n", av_err2str(ret));
			return ret;
		}
		/* Some video decoders decode only part of the packet, and have to be
		* called again with the remainder of the packet data.
		* Sample: fate-suite/lossless-audio/luckynight-partial.shn
		* Also, some decoders might over-read the packet. */
		// TODO validate if this is true for video decoders too (it is for audio decoders)
		decoded = FFMIN(ret, pi->pkt.size);

		if (*got_video_frame && DEBUG) {
			printf("packet dts:%s pts:%s duration:%s\n",
				av_ts2timestr(pi->pkt.dts, &pi->video_stream->time_base),
				av_ts2timestr(pi->pkt.pts, &pi->video_stream->time_base),
				av_ts2timestr(pi->pkt.duration, &pi->video_stream->time_base));

			printf("video_frame%s n:%d nb_samples:%d pts:%s\n",
				cached ? "(cached)" : "",
				video_frame_count++, pi->frame->nb_samples,
				av_ts2timestr(pi->frame->pts, &pi->audio_stream->time_base));
		}
	}

	return decoded;
}

static int convert_audio_samples(ProxyInstance *pi) {
	/* prepare/update sample format conversion buffer */
	int output_buffer_size_needed = pi->frame->nb_samples * pi->frame->channels * av_get_bytes_per_sample(pi->audio_codec_ctx->sample_fmt);
	if (pi->output_buffer_size < output_buffer_size_needed) {
		fprintf(stderr, "output buffer too small (%d < %d)\n", pi->output_buffer_size, output_buffer_size_needed);
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

static int convert_video_frame(ProxyInstance *pi) {
	/* convert frame to target format */
	int ret = sws_scale(pi->sws, pi->frame->data, pi->frame->linesize, 0, pi->video_codec_ctx->height, pi->video_picture.data, pi->video_picture.linesize);
	if (ret < 0) {
		fprintf(stderr, "Could not convert frame\n");
	}

	// VERY VERBOSE DEBUG: print monochromatic scaled down frame picture to console
	if (DEBUG && 0) {
		const char *QUANT_STEPS = " .:ioIX";

		for (int y = 0; y < pi->video_codec_ctx->height; y += pi->video_codec_ctx->height / 20) {
			for (int x = 0; x < pi->video_codec_ctx->width; x += pi->video_codec_ctx->width / 64) {
				printf("%c", QUANT_STEPS[(pi->video_picture.data[0][y * pi->video_picture.linesize[0] + x * 3 /* blue channel */]) / 40]);
			}
			printf("\n");
		}
		printf("\n");
	}

	return ret; // if >= 0, the height of the output frame
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

static inline int64_t pts_to_samples(ProxyInstance *pi, AVRational time_base, int64_t time)
{
	return (int64_t)round((av_q2d(time_base) * time) * pi->audio_output.format.sample_rate);
}

static inline int64_t samples_to_pts(ProxyInstance *pi, AVRational time_base, int64_t time)
{
	return (int64_t)round(time / av_q2d(time_base) / pi->audio_output.format.sample_rate);
}
