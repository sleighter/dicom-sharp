#region Copyright
// 
// This library is based on dcm4che see http://www.sourceforge.net/projects/dcm4che
// Copyright (c) 2002 by TIANI MEDGRAPH AG. All rights reserved.
//
// Modifications Copyright (C) 2002 Fang Yang. All rights reserved.
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

namespace org.dicomcs.data
{
	using System;
	
	public class DcmParserFactory
	{
		private static DcmParserFactory  s_instance = new DcmParserFactory();

		public static DcmParserFactory Instance
		{
			get { return s_instance; }			
		}

		/// <summary>
		/// Creates a new instance of DcmParserFactory
		/// </summary>
		private DcmParserFactory()
		{
		}

		public virtual DcmParser NewDcmParser(System.IO.Stream ins)
		{
			return new DcmParser(ins);
		}		
	}
}