#region Copyright
// 
// This library is based on dcm4che see http://www.sourceforge.net/projects/dcm4che
// Copyright (c) 2002 by TIANI MEDGRAPH AG. All rights reserved.
//
// Modifications Copyright (C) 2002,2008 Fang Yang. All rights reserved.
// 
// This file is part of dicomcs, see http://www.sourceforge.net/projects/dicom-cs
//
// This library is free software; you can redistribute it and/or modify it
// under the terms of the GNU Lesser General Public License as published
// by the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.                                 
// 
// This library is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//
// Fang Yang (yangfang@email.com)
//
#endregion

namespace test.dicomcs.data
{
	using System;
	using System.IO;
	using org.dicomcs.data;
	using org.dicomcs.dict;

	/// <summary>
	/// LoadDcmFile
	/// </summary>
	public class LoadDcmFile
	{
		public LoadDcmFile()
		{			
		}

		public void Load( String fileName )
		{
			Load( new FileInfo( fileName ) );
		}

		public void Load( FileInfo file )
		{
			Stream ins = null;
			DcmParser parser = null;
			Dataset ds = null;

			try
			{
				try
				{
					ins = new BufferedStream(new FileStream(file.FullName, FileMode.Open, FileAccess.Read));
					parser = new DcmParser(ins);
					FileFormat format = parser.DetectFileFormat();
					if (format != null)
					{
						ds = new Dataset();
						parser.DcmHandler = ds.DcmHandler;
						parser.ParseDcmFile(format, Tags.PixelData);

						Console.WriteLine( "success!" );
					}
					else
					{
						Console.WriteLine( "failed!" );
					}
				}
				catch( Exception e)
				{
					Console.WriteLine( e.StackTrace );
				}
			}
			finally
			{
				if (ins != null)
				{
					try
					{
						ins.Close();
					}
					catch (IOException)
					{
					}
				}
			}
		}

		public static void Main(string[] args)
		{
			LoadDcmFile loader = new LoadDcmFile();

			foreach (string path in args)
			{
				if (System.IO.File.Exists(path))
				{
					loader.Load(path);
				}
				else
				{
					DirectoryInfo dir = new DirectoryInfo( path );
					FileInfo[] files = dir.GetFiles();
					foreach (FileInfo file in files)
						loader.Load( file );	
				}
			}
		}
	}
}
