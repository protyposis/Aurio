add_library (aurioffmpegproxy SHARED "proxy.c" "proxy.h" "seekindex.c" "seekindex.h")
add_executable (aurioffmpegproxy_exe "main.c")
set_property(TARGET aurioffmpegproxy_exe PROPERTY OUTPUT_NAME aurioffmpegproxy)
target_link_libraries(aurioffmpegproxy_exe aurioffmpegproxy)

if (CMAKE_VERSION VERSION_GREATER 3.12)
  set_property(TARGET ${TARGET_NAME} PROPERTY CXX_STANDARD 20)
endif()

# FFmpeg
set(FFMPEG_PLATFORM win64)
set(FFMPEG_DIR ${CMAKE_CURRENT_SOURCE_DIR}/../../libs/ffmpeg/${FFMPEG_PLATFORM}/)
target_include_directories(aurioffmpegproxy PUBLIC ${FFMPEG_DIR}/include/)
target_link_libraries(aurioffmpegproxy PRIVATE 
	${FFMPEG_DIR}/lib/avcodec.lib
	${FFMPEG_DIR}/lib/avformat.lib
	${FFMPEG_DIR}/lib/avutil.lib
	${FFMPEG_DIR}/lib/swresample.lib
	${FFMPEG_DIR}/lib/swscale.lib
)
add_custom_command(TARGET aurioffmpegproxy POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
        ${FFMPEG_DIR}/bin/avcodec-60.dll
		${FFMPEG_DIR}/bin/avformat-60.dll
		${FFMPEG_DIR}/bin/avutil-58.dll
		${FFMPEG_DIR}/bin/swresample-4.dll
		${FFMPEG_DIR}/bin/swscale-7.dll
        $<TARGET_FILE_DIR:aurioffmpegproxy>
)
