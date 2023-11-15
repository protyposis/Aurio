$version = "ffmpeg-n6.0-latest-win64-lgpl-shared-6.0"
$localname = "win64"
$archive = "$version.zip"
$dest = ".\libs\ffmpeg"

wget -O $archive https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/$archive

Expand-Archive .\$archive -DestinationPath $dest

rm .\$archive

if (test-path $dest\$localname) {
  rm -r -force $dest\$localname
}

mv $dest\$version $dest\$localname