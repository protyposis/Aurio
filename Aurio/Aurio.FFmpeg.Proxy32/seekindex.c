// 
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2016  Mario Guggenberger <mg@protyposis.net>
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
#include <stdint.h>
#include <string.h>
#include <stdlib.h>

#include "seekindex.h"

#define BUILDER_INDEX_SIZE 100

static SeekIndexBuildHelper *builder_create();
static void builder_free(SeekIndexBuildHelper *builder);

/* 
 * Instantiates a seek index in build mode and returns it. 
 * To build the index, timestamps must be added sequentially through seekindex_build_add(), 
 * and after all timestamps have been added, the final index must be created with 
 * seekindex_build_finalize().
 * 
 * Because it is unknown how many timestamp will be added to the index, and thus
 * how large the index (lookup table) will be, and because we do not want to create
 * an incredibly huge index to cover all potential cases, the index is created
 * by linking small fixed-size partial indices together (similar to a forward linked list).
 * For later lookup operations, this linked index is then merged into a single 
 * index for easier processing and saving scanning through the linked index parts
 * on every access / lookup.
 */
SeekIndex *seekindex_build() {
	SeekIndex *si;

	// Alloc instance
	si = malloc(sizeof(SeekIndex));

	// Set initial values
	si->index = NULL;

	// Add the first builder
	si->builder_first = builder_create();
	si->builder_current = si->builder_first;

	return si;
}

static SeekIndexBuildHelper *builder_create() {
	SeekIndexBuildHelper *builder;

	// Alloc instance
	builder = malloc(sizeof(SeekIndexBuildHelper));

	// Set initial values
	builder->next = NULL;
	builder->size = BUILDER_INDEX_SIZE;
	builder->fill_level = 0;

	// Alloc index space
	builder->index = malloc(sizeof(int64_t) * builder->size);

	return builder;
}

static void builder_free(SeekIndexBuildHelper *builder) {
	SeekIndexBuildHelper *builder_next;

	// Free builder and all linked builders
	while (builder != NULL) {
		builder_next = builder->next; // Save reference to temp variable before freeing

		free(builder->index);
		free(builder);

		builder = builder_next; // Switch to next builder
	}
}

/*
 * Adds a timestamp to the index. Timestamps must be added sequentially.
 */
void seekindex_build_add(SeekIndex *si, int64_t timestamp) {
	// Increase builder index size if necessary (if current builder is full)
	if (si->builder_current->fill_level == si->builder_current->size) {
		// The current builder is full, add the next one
		si->builder_current->next = builder_create();
		// Set the next one as the current one
		si->builder_current = si->builder_current->next;
	}

	// Add the timestamp to the index and increase the fill level
	si->builder_current->index[si->builder_current->fill_level++] = timestamp;
}

/*
 * Finalizes the index by converting it from the temporary linked table in build mode
 * to a static continuous table.
 */
void seekindex_build_finalize(SeekIndex *si) {
	SeekIndexBuildHelper *builder;
	size_t total_size = 0;

	// Iterate through all linked builders and sum up the total index size
	builder = si->builder_first;
	while (builder != NULL) {
		total_size += builder->fill_level;
		builder = builder->next; // Switch to next builder
	}

	// Allow memory for the full index
	si->index = malloc(sizeof(int64_t) * total_size);
	si->size = total_size;

	// Transfer partial indices to the full index
	builder = si->builder_first;
	size_t offset = 0;
	while (builder != NULL) {
		memcpy(&si->index[offset], builder->index, builder->fill_level * sizeof(int64_t));
		offset += builder->fill_level;
		builder = builder->next; // Switch to next builder
	}

	// Remove and free builder
	builder_free(si->builder_first);
	si->builder_first = si->builder_current = NULL;
}

/*
 * Finds the timestamp in the index that covers the given timestamp and returns
 * a status code (0 on success, a negative number on error).
 */
int seekindex_find(SeekIndex *si, int64_t timestamp, int64_t *index_timestamp) {
	size_t left, right, mid;

	if (si->index == NULL) {
		fprintf(stderr, "index not finalized\n");
		return -1;
	}

	// Init binary search boundaries
	left = 0;
	right = si->size - 1;

	if (timestamp < si->index[left]) {
		return -2;
	}

	while (1) {
		mid = left + ((right - left) / 2); // calculate the middle item to test

		//printf("l/m/r: %zu/%zu/%zu (%lld/%lld/%lld, %lld)\n", left, mid, right, 
		//	si->index[left], si->index[mid], si->index[right], timestamp);

		if (left == right) {
			*index_timestamp = si->index[mid];
			return 0;
		}
		else if (right - left == 1) {
			if (timestamp >= si->index[right]) {
				left++;
			}
			else {
				right--;
			}
		}
		else if (timestamp < si->index[mid]) {
			right = mid;
		}
		else {
			left = mid;
		}
	}

	return -3;
}

/*
 * Frees all memory of the index.
*/
void seekindex_free(SeekIndex *si) {
	if (si->builder_first != NULL) {
		builder_free(si->builder_first);
	}
	if(si->index != NULL) {
		free(si->index);
	}
	free(si);
}

void seekindex_test() {
	SeekIndex *si;
	int64_t interval = 2500; // results in 3 linked table parts
	int64_t increment = 10;

	si = seekindex_build();

	for (int64_t x = 0; x < interval; x += increment) {
		seekindex_build_add(si, x);
		printf("adding %lld\n", x);
	}

	seekindex_build_finalize(si);

	int64_t output = 0;
	for (int64_t x = -5; x < interval + 5; x++) {
		int status = seekindex_find(si, x, &output);
		printf("lookup of %lld resulted in %lld (status %d)\n", x, output, status);
	}

	seekindex_free(si);
}