// 
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2018  Mario Guggenberger <mg@protyposis.net>
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Aurio.Streams;

namespace Aurio.FFmpeg
{
    public class FFmpegAudioStreamFactory : IAudioStreamFactory
    {
        public IAudioStream OpenFile(FileInfo fileInfo)
        {
            if (FFmpegSourceStream.WaveProxySuggested(fileInfo))
            {
                Console.WriteLine("File format with known seek problems, creating proxy file...");
                return AudioStreamFactory.FromFileInfo(FFmpegSourceStream.CreateWaveProxy(fileInfo));
            }
            else
            {
                try
                {
                    FFmpegSourceStream stream = new FFmpegSourceStream(fileInfo);

                    // Make a seek to test if it works or if it throws an exception
                    stream.Position = 0;

                    return stream;
                }
                catch (FFmpegSourceStream.FileNotSeekableException)
                {
                    /* 
                     * This exception gets thrown if a file is not seekable and therefore cannot
                     * provide all the functionality that is needed for an IAudioStream, although
                     * the problem could be solved by creating a seek index. See FFmpegSourceStream
                     * for further information.
                     * 
                     * For now, we create a WAV proxy file, because it is easier (consumes 
                     * additional space though).
                     */
                    Console.WriteLine("File not seekable, creating proxy file...");
                    return AudioStreamFactory.FromFileInfo(FFmpegSourceStream.CreateWaveProxy(fileInfo));
                }
                catch (FFmpegSourceStream.FileSeekException)
                {
                    /* 
                     * This exception gets thrown if a file should be seekable but seeking still does 
                     * not work correctly. We also create a proxy in this case.
                     */
                    Console.WriteLine("File test seek failed, creating proxy file...");
                    return AudioStreamFactory.FromFileInfo(FFmpegSourceStream.CreateWaveProxy(fileInfo));
                }
                catch (DllNotFoundException e)
                {
                    throw new DllNotFoundException("Cannot open file through FFmpeg: DLL missing", e);
                }
            }
        }
    }
}
