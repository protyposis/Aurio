#pragma once

typedef struct SeekIndexBuildHelper {
	struct SeekIndexBuildHelper	*next;
	int64_t					*index;
	size_t					size;
	size_t					fill_level;
} SeekIndexBuildHelper;

typedef struct SeekIndex {
	int64_t				*index; // contains the list of seekable PTS
	size_t				size; // The number of intems in the index

							  // fields required for building the index
	SeekIndexBuildHelper	*builder_first; // the first index build helper
	SeekIndexBuildHelper	*builder_current; // the current ibh, so we don't have to go through all references at every insert
} SeekIndex;

SeekIndex *seekindex_build();
void seekindex_build_add(SeekIndex *si, int64_t timestamp);
void seekindex_build_finalize(SeekIndex *si);
int seekindex_find(SeekIndex *si, int64_t timestamp, int64_t *index_timestamp);
void seekindex_free(SeekIndex *si);
void seekindex_test();