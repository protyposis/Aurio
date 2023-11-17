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
#include "proxy.h"

FILE* file_open(const char* filename) {
	return fopen(filename, "rb");
}

void file_rewind(FILE* f) {
	rewind(f);
}

int file_close(FILE* f) {
	return fclose(f);
}

int file_read_packet(FILE* f, uint8_t* buf, int buf_size) {
	return (int)fread(buf, 1, buf_size, f);
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

int main(int argc, char* argv[])
{
	ProxyInstance* pi;
	int64_t timestamp;
	int ret;
	uint8_t* output_buffer;
	int audio_output_buffer_size = 0, video_output_buffer_size = 0, output_buffer_size;
	int frame_type;
	struct VideoFrameProps* video_frame_props = NULL;

	const int stream_mode = 1; // 0 =  file, 1 = buffered stream IO
	FILE* f = NULL; // used for buffered stream IO
	int mode = TYPE_AUDIO | TYPE_VIDEO;

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
		pi = stream_open_bufferedio(mode, f, file_read_packet, file_seek, argv[1]);

		if (stream_has_error(pi)) {
			stream_get_error(pi);
			stream_close(pi);
			exit(1);
		}
	}
	else { // file IO
		pi = stream_open_file(mode, argv[1]);
	}

	//info(pi->fmt_ctx);

	if (mode & TYPE_AUDIO) {
		printf("audio length: %lld, frame size: %d\n", pi->audio_output.length, pi->audio_output.frame_size);
		printf("audio format (samplerate/samplesize/channels): %d/%d/%d\n",
			pi->audio_output.format.sample_rate, pi->audio_output.format.sample_size, pi->audio_output.format.channels);

		audio_output_buffer_size = pi->audio_output.frame_size * pi->audio_output.format.channels * pi->audio_output.format.sample_size;
	}
	if (mode & TYPE_VIDEO) {
		printf("video length: %lld, frame size: %d\n", pi->video_output.length, pi->video_output.frame_size);
		printf("video format (width/height/fps/aspect): %d/%d/%f/%f\n",
			pi->video_output.format.width, pi->video_output.format.height, pi->video_output.format.frame_rate, pi->video_output.format.aspect_ratio);

		video_output_buffer_size = pi->video_output.frame_size;
	}
	output_buffer_size = max(audio_output_buffer_size, video_output_buffer_size);
	output_buffer = malloc(output_buffer_size);

	// read full stream
	int64_t count1 = 0, last_ts1 = -1;
	while ((ret = stream_read_frame(pi, &timestamp, output_buffer, output_buffer_size, &frame_type)) >= 0) {
		printf("read %d @ %lld type %d\n", ret, timestamp, frame_type);
		if (frame_type == TYPE_VIDEO) {
			printf("keyframe %d, pict_type %d, interlaced %d, top_field_first %d\n",
				pi->video_output.current_frame.keyframe, pi->video_output.current_frame.pict_type,
				pi->video_output.current_frame.interlaced, pi->video_output.current_frame.top_field_first);
		}
		count1++;
		last_ts1 = timestamp;
	}

	printf("creating seekindex... ");
	stream_seekindex_create(pi, mode);
	printf("done\n");

	// seek back to start
	stream_seek(pi, 0, mode == TYPE_VIDEO ? TYPE_VIDEO : TYPE_AUDIO);

	// read again (output should be the same as above)
	int64_t count2 = 0, last_ts2 = -1;
	int64_t accumulated_frame_length = 0;
	int last_ret = 0;
	while ((ret = stream_read_frame(pi, &timestamp, output_buffer, output_buffer_size, &frame_type)) >= 0) {
		printf("read %d @ %lld type %d\n", ret, timestamp, frame_type);
		count2++;
		last_ts2 = timestamp;
		if (frame_type == TYPE_AUDIO) {
			accumulated_frame_length += ret;
			last_ret = ret;
		}
	}
	int64_t length_from_last_ts = last_ts2 + last_ret; // last timestamp + frame length

	printf("read1 count: %lld, timestamp: %lld\n", count1, last_ts1);
	printf("read2 count: %lld, timestamp: %lld\n", count2, last_ts2);

	if (mode & TYPE_AUDIO) {
		// Print lengths from 
		// - the header, 
		// - summed over all frames, 
		// - and from the last timestamp + frame length
		// to compare for inconsistencies.
		printf("audio length header/accumulated/last_ts: %lld/%lld/%lld\n",
			pi->audio_output.length, accumulated_frame_length, length_from_last_ts);
	}

	free(output_buffer);

	stream_close(pi);

	if (stream_mode) {
		file_close(f);
	}

	return 0;
}