# Force library to scan local directory for FFmpeg libs. Without it, `LD_LIBRARY_PATH=.` is needed.
# This must come before add_library/add_executable
set(CMAKE_BUILD_RPATH "$ORIGIN")

add_library (aurioffmpegproxy SHARED "proxy.c" "proxy.h" "seekindex.c" "seekindex.h")
add_executable (aurioffmpegproxy_exe "main.c")
set_property(TARGET aurioffmpegproxy_exe PROPERTY OUTPUT_NAME aurioffmpegproxy)
target_link_libraries(aurioffmpegproxy_exe aurioffmpegproxy)

if (CMAKE_VERSION VERSION_GREATER 3.12)
  set_property(TARGET ${TARGET_NAME} PROPERTY CXX_STANDARD 20)
endif()

if (WIN32)
	set(FFMPEG_PLATFORM win64)
	set(LIB_PREFIX "")
	set(LIB_EXT .lib)
else()
	set(FFMPEG_PLATFORM linux64)
	set(LIB_PREFIX "lib")
	set(LIB_EXT .so)
endif()

# No PkgConfig on Windows, and the hacky workaround works on Windows and Linux.
#find_package(PkgConfig REQUIRED)
#set(CMAKE_PREFIX_PATH "${CMAKE_CURRENT_SOURCE_DIR}/../../libs/ffmpeg/${FFMPEG_PLATFORM}/")
#pkg_check_modules(LIBAV REQUIRED IMPORTED_TARGET
#    libavcodec
#    libavformat
#    libavutil
#    libswresample
#    libswscale
#)
#target_link_libraries(aurioffmpegproxy PkgConfig::LIBAV)

set(FFMPEG_DIR ${CMAKE_CURRENT_SOURCE_DIR}/../../libs/ffmpeg/${FFMPEG_PLATFORM}/)
target_include_directories(aurioffmpegproxy PUBLIC ${FFMPEG_DIR}/include/)
target_link_libraries(aurioffmpegproxy PRIVATE 
	${FFMPEG_DIR}/lib/${LIB_PREFIX}avcodec${LIB_EXT}
	${FFMPEG_DIR}/lib/${LIB_PREFIX}avformat${LIB_EXT}
	${FFMPEG_DIR}/lib/${LIB_PREFIX}avutil${LIB_EXT}
	${FFMPEG_DIR}/lib/${LIB_PREFIX}swresample${LIB_EXT}
	${FFMPEG_DIR}/lib/${LIB_PREFIX}swscale${LIB_EXT}
)

# Copy libraries to build output directory
if (WIN32)
	add_custom_command(TARGET aurioffmpegproxy POST_BUILD
		COMMAND ${CMAKE_COMMAND} -E copy_if_different
			${FFMPEG_DIR}/bin/avcodec-60.dll
			${FFMPEG_DIR}/bin/avformat-60.dll
			${FFMPEG_DIR}/bin/avutil-58.dll
			${FFMPEG_DIR}/bin/swresample-4.dll
			${FFMPEG_DIR}/bin/swscale-7.dll
			$<TARGET_FILE_DIR:aurioffmpegproxy>
	)
else()
	add_custom_command(TARGET aurioffmpegproxy POST_BUILD
		COMMAND ${CMAKE_COMMAND} -E copy_if_different
			${FFMPEG_DIR}/lib/${LIB_PREFIX}avcodec${LIB_EXT}.60
			${FFMPEG_DIR}/lib/${LIB_PREFIX}avformat${LIB_EXT}.60
			${FFMPEG_DIR}/lib/${LIB_PREFIX}avutil${LIB_EXT}.58
			${FFMPEG_DIR}/lib/${LIB_PREFIX}swresample${LIB_EXT}.4
			${FFMPEG_DIR}/lib/${LIB_PREFIX}swscale${LIB_EXT}.7
			$<TARGET_FILE_DIR:aurioffmpegproxy>
	)
endif()
