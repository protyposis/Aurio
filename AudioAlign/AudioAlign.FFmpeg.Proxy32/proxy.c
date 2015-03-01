#include <stdio.h>

/* Since the Microsoft C compiler does not support the inline keyword which is used by FFmpeg,
* it needs to be translated to the Microsft __inline extension, which is the same.
* http://stackoverflow.com/a/24435157
*/
#ifdef _MSC_VER
	#define inline __inline
#endif

// 
#include "libavcodec\avcodec.h"

#pragma comment(lib, "avcodec.lib")

int main(int argc, char *argv[])
{
	avcodec_register_all();
	printf("Hello FFmpeg!\n");
	return 0;
}

