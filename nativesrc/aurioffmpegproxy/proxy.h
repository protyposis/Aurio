// 
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2023  Mario Guggenberger <mg@protyposis.net>
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

// System includes
#include <stdio.h>

// FFmpeg includes
#include "libavcodec/avcodec.h"
#include "libavformat/avformat.h"
#include "libavutil/timestamp.h"
#include "libswresample/swresample.h"
#include "libavutil/opt.h"
#include "libswscale/swscale.h"

#include "seekindex.h"

/*
 * This struct holds all data necessary to manage an "instance" of a decoder,
 * and most importantly to run several decoders in parallel.
 */
typedef struct ProxyInstance {
	int					mode; // contains the desired packet types to decode
	int					state;
	char* error_message; // in case of state == PI_STATE_ERROR
	AVFormatContext* fmt_ctx;
	AVStream* audio_stream;
	AVStream* video_stream;
	AVCodecContext* audio_codec_ctx;
	AVCodecContext* video_codec_ctx;
	AVPacket* pkt;
	AVFrame* frame;
	SwrContext* swr;
	struct SwsContext* sws;
	int					output_buffer_size;
	uint8_t* output_buffer;
	int64_t				frame_pts;
	SeekIndex* audio_seekindex;
	SeekIndex* video_seekindex;

	struct {
		struct {
			int					sample_rate;
			int					sample_size; // bytes per sample (2 bytes for 16 bit int, 4 bytes for 32 bit float)
			int					channels;
		}					format;
		int64_t				length;
		int					frame_size;
		int64_t				sample_position;
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
		struct VideoFrameProps {
			int					keyframe;
			enum AVPictureType	pict_type;
			int					interlaced;
			int					top_field_first;
		}					current_frame;
		int64_t				sample_position;
	}					video_output;
} ProxyInstance;

#if defined(_MSC_VER)
	#define EXPORT __declspec(dllexport)
#else
	// https://gcc.gnu.org/wiki/Visibility
	#define EXPORT __attribute__ ((visibility ("default")))
#endif

#define DEBUG 0

#define TYPE_NONE  0x00
#define TYPE_AUDIO 0x01
#define TYPE_VIDEO 0x02

#define PI_STATE_OK 0
#define PI_STATE_ERROR -1

EXPORT ProxyInstance* stream_open_file(int mode, char* filename);
EXPORT ProxyInstance* stream_open_bufferedio(int mode, void* opaque, int(*read_packet)(void* opaque, uint8_t* buf, int buf_size), int64_t(*seek)(void* opaque, int64_t offset, int whence), char* filename);
ProxyInstance* stream_open(ProxyInstance* pi);
EXPORT void* stream_get_output_config(ProxyInstance* pi, int type);
int stream_read_frame_any(ProxyInstance* pi, int* got_frame, int* frame_type);
EXPORT int stream_read_frame(ProxyInstance* pi, int64_t* timestamp, uint8_t* output_buffer, int output_buffer_size, int* frame_type);
EXPORT void stream_seek(ProxyInstance* pi, int64_t timestamp, int type);
EXPORT void stream_seekindex_create(ProxyInstance* pi, int type);
EXPORT void stream_seekindex_remove(ProxyInstance* pi, int type);
EXPORT void stream_close(ProxyInstance* pi);
EXPORT int stream_has_error(ProxyInstance* pi);
EXPORT char* stream_get_error(ProxyInstance* pi);

static int pi_init(ProxyInstance** pi);
static void pi_free(ProxyInstance** pi);
static void pi_set_error(ProxyInstance* pi, const char* fmt, ...);
static int pi_has_error(ProxyInstance* pi);

static void info(AVFormatContext* fmt_ctx);
static int open_codec_context(AVFormatContext* fmt_ctx, AVCodecContext** codec_ctx, int type);
static int decode_audio_packet(ProxyInstance* pi, int* got_audio_frame, int cached);
static int decode_video_packet(ProxyInstance* pi, int* got_video_frame, int cached);
static int convert_audio_samples(ProxyInstance* pi);
static int convert_video_frame(ProxyInstance* pi);
static int determine_target_format(AVCodecContext* audio_codec_ctx);
static inline int64_t pts_to_samples(double sample_rate, AVRational time_base, int64_t time);
static inline int64_t samples_to_pts(double sample_rate, AVRational time_base, int64_t time);