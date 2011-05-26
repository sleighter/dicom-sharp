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

namespace test.dicomcs.net
{
	using System;
	using System.IO;
	using System.Reflection;
	using System.Threading;
	using System.Net.Sockets;
	using org.dicomcs.dict;
	using org.dicomcs.data;
	using org.dicomcs.net;

	/// <summary>
	/// Summary description for TestSCU.
	/// </summary>
	public class TestSCU
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private static AssociationFactory aFact = AssociationFactory.Instance;
		private static DcmObjectFactory oFact= DcmObjectFactory.Instance;
		private static DcmParserFactory pFact= DcmParserFactory.Instance;

		private static String[] DEF_TS = new String[]{UIDs.ImplicitVRLittleEndian};
		private const int PCID_START = 1;

		private AAssociateRQ m_assocRQ = aFact.NewAAssociateRQ();
		private String m_host = "localhost";
		private int    m_port = 104;
		private int m_assocTimeOut = 0;

		public TestSCU(String callingAET, String calledAET, String host, int port )
		{
			m_assocRQ.CalledAET = calledAET;
			m_assocRQ.CallingAET = callingAET;
			m_assocRQ.AsyncOpsWindow = aFact.NewAsyncOpsWindow(0, 1);
			m_assocRQ.MaxPduLength = 16352;
			m_host = host;
			m_port = port;
		}

		private ActiveAssociation OpenAssoc( )
		{
			Association assoc = aFact.NewRequestor(new TcpClient(m_host, m_port));
			PduI assocAC = assoc.Connect(m_assocRQ, m_assocTimeOut);
			if (!(assocAC is AAssociateAC))
			{
				return null;
			}

			ActiveAssociation active = aFact.NewActiveAssociation(assoc, null);
			active.Start();
			return active;
		}

		/// <summary>
		/// Send C-ECHO
		/// </summary>
		public void CEcho()
		{
			m_assocRQ.AddPresContext(aFact.NewPresContext(PCID_START, UIDs.Verification, DEF_TS));
			ActiveAssociation active = OpenAssoc();
			if (active != null)
			{
				if (active.Association.GetAcceptedTransferSyntaxUID(PCID_START) == null)
				{
					log.Error( "Verification SOP class is not supported" );
				}
				else
				{
					active.Invoke(aFact.NewDimse(PCID_START, oFact.NewCommand().InitCEchoRQ(0)), null);
				}
				
				active.Release(true);
			}
		}

		/// <summary>
		/// Send C-STORE
		/// </summary>
		/// <param name="fileName"></param>
		public void CStore( String fileName )
		{
			Stream ins = null;
			DcmParser parser = null;
			Dataset ds = null;

			try
			{
				//
				// Load DICOM file
				//
				FileInfo file = new FileInfo( fileName );
				try
				{
					ins = new BufferedStream(new FileStream( fileName, FileMode.Open, FileAccess.Read));
					parser = pFact.NewDcmParser(ins);
					FileFormat format = parser.DetectFileFormat();
					if (format != null)
					{
						ds = oFact.NewDataset();
						parser.DcmHandler = ds.DcmHandler;
						parser.ParseDcmFile(format, Tags.PixelData);
						log.Debug( "Reading done" );
					}
					else
					{
						log.Error( "Unknown format!" );
					}
				}
				catch (IOException e)
				{
					log.Error( "Reading failed", e );
				}

				//
				// Prepare association
				//
				String classUID = ds.GetString( Tags.SOPClassUID);
				String tsUID = ds.GetString(Tags.TransferSyntaxUID);
				if( tsUID == null || tsUID.Equals( "" ) )
					tsUID = UIDs.ImplicitVRLittleEndian;

				m_assocRQ.AddPresContext(aFact.NewPresContext(PCID_START+2, 
							classUID, 
							new String[]{ tsUID } ));
				ActiveAssociation active = OpenAssoc();
				if (active != null)
				{
					SendDataset(active, file, parser, ds);
					active.Release(true);
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
	
		private bool SendDataset(ActiveAssociation active, FileInfo file, DcmParser parser, Dataset ds)
		{
			String sopInstUID = ds.GetString(Tags.SOPInstanceUID);
			if (sopInstUID == null)
			{
				log.Error( "SOP instance UID is null" );
				return false;
			}
			String sopClassUID = ds.GetString(Tags.SOPClassUID);
			if (sopClassUID == null)
			{
				log.Error( "SOP class UID is null" );
				return false;
			}
			PresContext pc = null;
			Association assoc = active.Association;
			if (parser.DcmDecodeParam.encapsulated)
			{
				String tsuid = ds.GetFileMetaInfo().TransferSyntaxUID;
				if ((pc = assoc.GetAcceptedPresContext(sopClassUID, tsuid)) == null)
				{
					log.Error( "SOP class UID not supported" );
					return false;
				}
			}
			else if ((pc = assoc.GetAcceptedPresContext(sopClassUID, UIDs.ImplicitVRLittleEndian)) == null && (pc = assoc.GetAcceptedPresContext(sopClassUID, UIDs.ExplicitVRLittleEndian)) == null && (pc = assoc.GetAcceptedPresContext(sopClassUID, UIDs.ExplicitVRBigEndian)) == null)
			{
				log.Error( "SOP class UID not supported" );
				return false;
			}

			active.Invoke(aFact.NewDimse(pc.pcid(), oFact.NewCommand().InitCStoreRQ(assoc.NextMsgID(), sopClassUID, sopInstUID, 0), new FileDataSource(parser, ds, new byte[2048])), null);
			return true;
		}

		/// <summary>
		/// File Data source
		/// </summary>
		public sealed class FileDataSource : DataSourceI
		{
			private DcmParser parser;
			private Dataset ds;
			private byte[] buffer;

			public FileDataSource(DcmParser parser, Dataset ds, byte[] buffer)
			{
				this.parser = parser;
				this.ds = ds;
				this.buffer = buffer;
			}
			
			public void  WriteTo(Stream outs, String tsUID)
			{
				DcmEncodeParam netParam = (DcmEncodeParam) DcmDecodeParam.ValueOf(tsUID);
				ds.WriteDataset(outs, netParam);
				if (parser.ReadTag == Tags.PixelData)
				{
					DcmDecodeParam fileParam = parser.DcmDecodeParam;
					ds.WriteHeader(outs, netParam, parser.ReadTag, parser.ReadVR, parser.ReadLength);
					if (netParam.encapsulated)
					{
						parser.ParseHeader();
						while (parser.ReadTag == Tags.Item)
						{
							ds.WriteHeader(outs, netParam, parser.ReadTag, parser.ReadVR, parser.ReadLength);
							copy(parser.InputStream, outs, parser.ReadLength, false, buffer);
						}
						if (parser.ReadTag != Tags.SeqDelimitationItem)
						{
							throw new DcmParseException("Unexpected Tag:" + Tags.ToHexString(parser.ReadTag));
						}
						if (parser.ReadLength != 0)
						{
							throw new DcmParseException("(fffe,e0dd), Length:" + parser.ReadLength);
						}
						ds.WriteHeader(outs, netParam, Tags.SeqDelimitationItem, VRs.NONE, 0);
					}
					else
					{
						bool swap = fileParam.byteOrder != netParam.byteOrder && parser.ReadVR == VRs.OW;
						copy(parser.InputStream, outs, parser.ReadLength, swap, buffer);
					}
					ds.Clear();
					parser.ParseDataset(fileParam, 0);
					ds.WriteDataset(outs, netParam);
				}
			}
		}
	
		private static void  copy(Stream ins, Stream outs, int len, bool swap, byte[] buffer)
		{
			if (swap && (len & 1) != 0)
			{
				throw new DcmParseException("Illegal length of OW Pixel Data: " + len);
			}
			if (buffer == null)
			{
				if (swap)
				{
					int tmp;
					for (int i = 0; i < len; ++i, ++i)
					{
						tmp = ins.ReadByte();
						outs.WriteByte((System.Byte) ins.ReadByte());
						outs.WriteByte((System.Byte) tmp);
					}
				}
				else
				{
					for (int i = 0; i < len; ++i)
					{
						outs.WriteByte((System.Byte) ins.ReadByte());
					}
				}
			}
			else
			{
				byte tmp;
				int c, remain = len;
				while (remain > 0)
				{
					c = ins.Read( buffer, 0, System.Math.Min(buffer.Length, remain));
					if (swap)
					{
						if ((c & 1) != 0)
						{
							buffer[c++] = (byte) ins.ReadByte();
						}
						for (int i = 0; i < c; ++i, ++i)
						{
							tmp = buffer[i];
							buffer[i] = buffer[i + 1];
							buffer[i + 1] = tmp;
						}
					}
					outs.Write(buffer, 0, c);
					remain -= c;
				}
			}
		}

		///////////////////////////////////////////////////////////////////////
		///////////////////////////////////////////////////////////////////////
		///////////////////////////////////////////////////////////////////////

		public static void Main()
		{
			try
			{
				BasicSCU scu = new BasicSCU( "NEWTON", "STORAGE_SCP", "localhost", 104 );
				
				//scu.CEcho();

				scu.CStore( @"d:\My Documents\Projecten\ECGTool\DICOM\DICOM (Resting) New\BARRY^BROWN20071003145855.dcm" );
			}
			catch( Exception e )
			{
				log.Error( e );
			}
		}
	}
}
