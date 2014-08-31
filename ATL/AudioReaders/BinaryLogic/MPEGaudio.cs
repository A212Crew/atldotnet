using ATL.Logging;
using System;
using System.IO;

namespace ATL.AudioReaders.BinaryLogic
{
    /// <summary>
    /// Class for MPEG Audio Layer files manipulation (extensions : .MP1, .MP2, .MP3)
    /// </summary>
	class TMPEGaudio : AudioDataReader
	{

		// Table for bit rates (KBit/s)
		public static ushort[,,] MPEG_BIT_RATE = new ushort[4,4,16]
   {
	   // For MPEG 2.5
		{
			{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
			{0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0},
			{0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0},
			{0, 32, 48, 56, 64, 80, 96, 112, 128, 144, 160, 176, 192, 224, 256, 0}
		},
	   // Reserved
		{
			{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
			{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
			{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
			{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
		},
	   // For MPEG 2
		{
			{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
			{0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0},
			{0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0},
			{0, 32, 48, 56, 64, 80, 96, 112, 128, 144, 160, 176, 192, 224, 256, 0}
		},
	   // For MPEG 1
		{
			{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
			{0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 0},
			{0, 32, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 384, 0},
			{0, 32, 64, 96, 128, 160, 192, 224, 256, 288, 320, 352, 384, 416, 448, 0}
		}
   };

		// Sample rate codes
		public const byte MPEG_SAMPLE_RATE_LEVEL_3 = 0;                    // Level 3
		public const byte MPEG_SAMPLE_RATE_LEVEL_2 = 1;                    // Level 2
		public const byte MPEG_SAMPLE_RATE_LEVEL_1 = 2;                    // Level 1
		public const byte MPEG_SAMPLE_RATE_UNKNOWN = 3;              // Unknown value

		// Table for sample rates
		public static ushort[,] MPEG_SAMPLE_RATE = new ushort[4,4]
	{
		{11025, 12000, 8000, 0},                                   // For MPEG 2.5
		{0, 0, 0, 0},                                                  // Reserved
		{22050, 24000, 16000, 0},                                    // For MPEG 2
		{44100, 48000, 32000, 0}                                     // For MPEG 1
	};

		// VBR header ID for Xing/FhG
		public const String VBR_ID_XING = "Xing";                       // Xing VBR ID
		public const String VBR_ID_FHG = "VBRI";                         // FhG VBR ID

		// MPEG version codes
		public const byte MPEG_VERSION_2_5 = 0;                            // MPEG 2.5
		public const byte MPEG_VERSION_UNKNOWN = 1;                 // Unknown version
		public const byte MPEG_VERSION_2 = 2;                                // MPEG 2
		public const byte MPEG_VERSION_1 = 3;                                // MPEG 1

		// MPEG version names
		public String[] MPEG_VERSION = new String[4]
	{"MPEG 2.5", "MPEG ?", "MPEG 2", "MPEG 1"};

		// MPEG layer codes
		public const byte MPEG_LAYER_UNKNOWN = 0;                     // Unknown layer
		public const byte MPEG_LAYER_III = 1;                             // Layer III
		public const byte MPEG_LAYER_II = 2;                               // Layer II
		public const byte MPEG_LAYER_I = 3;                                 // Layer I

		// MPEG layer names
		public String[] MPEG_LAYER = new String[4]
	{"Layer ?", "Layer III", "Layer II", "Layer I"};

		// Channel mode codes
		public const byte MPEG_CM_STEREO = 0;                                // Stereo
		public const byte MPEG_CM_JOINT_STEREO = 1;                    // Joint Stereo
		public const byte MPEG_CM_DUAL_CHANNEL = 2;                    // Dual Channel
		public const byte MPEG_CM_MONO = 3;                                    // Mono
		public const byte MPEG_CM_UNKNOWN = 4;                         // Unknown mode

		// Channel mode names
		public String[] MPEG_CM_MODE = new String[5]
	{"Stereo", "Joint Stereo", "Dual Channel", "Mono", "Unknown"};

		// Extension mode codes (for Joint Stereo)
		public const byte MPEG_CM_EXTENSION_OFF = 0;        // IS and MS modes set off
		public const byte MPEG_CM_EXTENSION_IS = 1;             // Only IS mode set on
		public const byte MPEG_CM_EXTENSION_MS = 2;             // Only MS mode set on
		public const byte MPEG_CM_EXTENSION_ON = 3;          // IS and MS modes set on
		public const byte MPEG_CM_EXTENSION_UNKNOWN = 4;     // Unknown extension mode

		// Emphasis mode codes
		public const byte MPEG_EMPHASIS_NONE = 0;                              // None
		public const byte MPEG_EMPHASIS_5015 = 1;                          // 50/15 ms
		public const byte MPEG_EMPHASIS_UNKNOWN = 2;               // Unknown emphasis
		public const byte MPEG_EMPHASIS_CCIT = 3;                         // CCIT J.17

		// Emphasis names
		public String[] MPEG_EMPHASIS = new String[4]
	{"None", "50/15 ms", "Unknown", "CCIT J.17"};

		// Encoder codes
		public const byte MPEG_ENCODER_UNKNOWN = 0;                // Unknown encoder
		public const byte MPEG_ENCODER_XING = 1;                              // Xing
		public const byte MPEG_ENCODER_FHG = 2;                                // FhG
		public const byte MPEG_ENCODER_LAME = 3;                              // LAME
		public const byte MPEG_ENCODER_BLADE = 4;                            // Blade
		public const byte MPEG_ENCODER_GOGO = 5;                              // GoGo
		public const byte MPEG_ENCODER_SHINE = 6;                            // Shine
		public const byte MPEG_ENCODER_QDESIGN = 7;                        // QDesign

		// Encoder names
		public String[] MPEG_ENCODER = new String[8]
	{"Unknown", "Xing", "FhG", "LAME", "Blade", "GoGo", "Shine", "QDesign"};

		// Xing/FhG VBR header data
		public class VBRData
		{
			public bool Found;                            // True if VBR header found
			public char[] ID = new char[4];            // Header ID: "Xing" or "VBRI"
			public int Frames;                              // Total number of frames
			public int Bytes;                                // Total number of bytes
			public byte Scale;                                  // VBR scale (1..100)
			public String VendorID;                         // Vendor ID (if present)

            public void Reset()
            {
                Found = false;
                Array.Clear(ID, 0, ID.Length);
                Frames = 0;
                Bytes = 0;
                Scale = 0;
                VendorID = "";
            }
		}

		// MPEG frame header data}
		public class FrameData
		{
			public bool Found;                                 // True if frame found
			public int Position;                        // Frame position in the file
			public ushort Size;                                 // Frame size (bytes)
			public bool Xing;                                 // True if Xing encoder
			public byte[] Data = new byte[4];          // The whole frame header data
			public byte VersionID;                                 // MPEG version ID
			public byte LayerID;                                     // MPEG layer ID
			public bool ProtectionBit;                    // True if protected by CRC
			public ushort BitRateID;                                   // Bit rate ID
			public ushort SampleRateID;                             // Sample rate ID
			public bool PaddingBit;                           // True if frame padded
			public bool PrivateBit;                              // Extra information
			public byte ModeID;                                    // Channel mode ID
			public byte ModeExtensionID;      // Mode extension ID (for Joint Stereo)
			public bool CopyrightBit;                    // True if audio copyrighted
			public bool OriginalBit;                        // True if original media
			public byte EmphasisID;                                    // Emphasis ID

            public void Reset()
            {
                Found = false;
                Position = 0;
                Size = 0;
                Xing = false;
                Array.Clear(Data, 0, Data.Length);
                VersionID = 0;
                LayerID = 0;
                ProtectionBit = false;
                BitRateID = 0;
                SampleRateID = 0;
                PaddingBit = false;
                PrivateBit = false;
                ModeID = 0;
                ModeExtensionID = 0;
                CopyrightBit = false;
                OriginalBit = false;
                EmphasisID = 0;

                VersionID = MPEG_VERSION_UNKNOWN;
                SampleRateID = MPEG_SAMPLE_RATE_UNKNOWN;
                ModeID = MPEG_CM_UNKNOWN;
                ModeExtensionID = MPEG_CM_EXTENSION_UNKNOWN;
                EmphasisID = MPEG_EMPHASIS_UNKNOWN;
            }
		}  
      
		private String FVendorID;
		private VBRData FVBR = new VBRData();
		private FrameData FFrame = new FrameData();
    
		public VBRData VBR // VBR header data
		{
			get { return this.FVBR; }
		}	
		public FrameData Frame // Frame header data
		{
			get { return this.FFrame; }
		}
        public String Version // MPEG version name
        {
            get { return this.FGetVersion(); }
        }
        public String Layer // MPEG layer name
        {
            get { return this.FGetLayer(); }
        }
        public ushort SampleRate // Sample rate (hz)
        {
            get { return this.FGetSampleRate(); }
        }
        public String ChannelMode // Channel mode name
        {
            get { return this.FGetChannelMode(); }
        }
        public String Emphasis // Emphasis name
        {
            get { return this.FGetEmphasis(); }
        }
        public long Frames // Total number of frames
        {
            get { return this.FGetFrames(); }
        }
        public byte EncoderID // Guessed encoder ID
        {
            get { return this.FGetEncoderID(); }
        }
        public String Encoder // Guessed encoder name
        {
            get { return this.FGetEncoder(); }
        }
        public bool Valid // True if MPEG file valid
        {
            get { return this.FGetValid(); }
        }


        public override bool IsVBR
		{
			get { return this.FVBR.Found; }
		}
        public override int CodecFamily
		{
			get { return AudioReaderFactory.CF_LOSSY; }
		}
        public override bool AllowsParsableMetadata
        {
            get { return true; }
        }
  
		// Limitation constants
		public const int MAX_MPEG_FRAME_LENGTH = 1729;      // Max. MPEG frame length
		public const int MIN_MPEG_BIT_RATE = 8;                // Min. bit rate value (KBit/s)
        public const int MAX_MPEG_BIT_RATE = 448;              // Max. bit rate value (KBit/s)
		public const double MIN_ALLOWED_DURATION = 0.1;   // Min. song duration value

		// VBR Vendor ID strings
		public const String VENDOR_ID_LAME = "LAME";                      // For LAME
		public const String VENDOR_ID_GOGO_NEW = "GOGO";            // For GoGo (New)
		public const String VENDOR_ID_GOGO_OLD = "MPGE";            // For GoGo (Old)

		// ********************* Auxiliary functions & voids ********************

		private static bool IsFrameHeader(byte[] HeaderData)
		{
			// Check for valid frame header
            return !(
                ((HeaderData[0] & 0xFF) != 0xFF) ||
                ((HeaderData[1] & 0xE0) != 0xE0) ||
                (((HeaderData[1] >> 3) & 3) == 1) ||
                (((HeaderData[1] >> 1) & 3) == 0) ||
                ((HeaderData[2] & 0xF0) == 0xF0) ||
                ((HeaderData[2] & 0xF0) == 0) ||
                (((HeaderData[2] >> 2) & 3) == 3) ||
                ((HeaderData[3] & 3) == 2)
                );
		}

		// ---------------------------------------------------------------------------

		private static void DecodeHeader(byte[] HeaderData, ref FrameData Frame)
		{
			// Decode frame header data
			Array.Copy(HeaderData, Frame.Data, 4);
			Frame.VersionID = (byte)((HeaderData[1] >> 3) & 3);
			Frame.LayerID = (byte)((HeaderData[1] >> 1) & 3);
			Frame.ProtectionBit = (HeaderData[1] & 1) != 1;
			Frame.BitRateID = (ushort)(HeaderData[2] >> 4);
			Frame.SampleRateID = (ushort)((HeaderData[2] >> 2) & 3);
			Frame.PaddingBit = ( ((HeaderData[2] >> 1) & 1) == 1);
			Frame.PrivateBit = ( (HeaderData[2] & 1) == 1);
			Frame.ModeID = (byte)((HeaderData[3] >> 6) & 3);
			Frame.ModeExtensionID = (byte)((HeaderData[3] >> 4) & 3);
			Frame.CopyrightBit = ( ((HeaderData[3] >> 3) & 1) == 1);
			Frame.OriginalBit = ( ((HeaderData[3] >> 2) & 1) == 1);
			Frame.EmphasisID = (byte)(HeaderData[3] & 3);
		}

		// ---------------------------------------------------------------------------

		private static bool ValidFrameAt(int Index, byte[] Data)
		{
			byte[] HeaderData = new byte[4];

/*
			HeaderData[0] = Data[Index];
			HeaderData[1] = Data[Index + 1];
			HeaderData[2] = Data[Index + 2];
			HeaderData[3] = Data[Index + 3];
*/
            Array.ConstrainedCopy(Data, Index, HeaderData, 0, 4);

            // Check for frame at given position
            return IsFrameHeader(HeaderData);
		}

		// ---------------------------------------------------------------------------

		private static byte GetCoefficient(FrameData Frame)
		{
			// Get frame size coefficient
			if (MPEG_VERSION_1 == Frame.VersionID)
				if (MPEG_LAYER_I == Frame.LayerID) return 48;
				else return 144;
			else
				if (MPEG_LAYER_I == Frame.LayerID) return 24;
			else if (MPEG_LAYER_II == Frame.LayerID) return 144;
			else return 72;
		}

		// ---------------------------------------------------------------------------

		private static ushort GetBitRate(FrameData Frame)
		{
			// Get bit rate
			return MPEG_BIT_RATE[Frame.VersionID, Frame.LayerID, Frame.BitRateID];
		}

		// ---------------------------------------------------------------------------

		private static ushort GetSampleRate(FrameData Frame)
		{
			// Get sample rate
			return MPEG_SAMPLE_RATE[Frame.VersionID, Frame.SampleRateID];
		}

		// ---------------------------------------------------------------------------

		private static byte GetPadding(FrameData Frame)
		{
			// Get frame padding
			if (Frame.PaddingBit)
				if (MPEG_LAYER_I == Frame.LayerID) return 4;
				else return 1;
			else return 0;
		}

		// ---------------------------------------------------------------------------

		private static ushort GetFrameLength(FrameData Frame)
		{
			ushort Coefficient;
			ushort BitRate;
			ushort SampleRate;
			ushort Padding;

			// Calculate MPEG frame length
			Coefficient = GetCoefficient(Frame);
			BitRate = GetBitRate(Frame);
			SampleRate = GetSampleRate(Frame);
			Padding = GetPadding(Frame);
		
			return (ushort)(Math.Floor((double)Coefficient * (double)BitRate * 1000 / SampleRate) + Padding); 
		}

		// ---------------------------------------------------------------------------

		private static bool IsXing(int Index, byte[] Data)
		{
			// Get true if Xing encoder
			return ( (Data[Index] == 0) &&
				(Data[Index + 1] == 0) &&
				(Data[Index + 2] == 0) &&
				(Data[Index + 3] == 0) &&
				(Data[Index + 4] == 0) &&
				(Data[Index + 5] == 0) );
		}

		// ---------------------------------------------------------------------------

		private static VBRData GetXingInfo(int Index, byte[] Data)
		{
			VBRData result = new VBRData();
	
			result.Found = true;
			result.ID = VBR_ID_XING.ToCharArray();
			result.Frames =
				Data[Index + 8] * 0x1000000 +
				Data[Index + 9] * 0x10000 +
				Data[Index + 10] * 0x100 +
				Data[Index + 11];
			result.Bytes =
				Data[Index + 12] * 0x1000000 +
				Data[Index + 13] * 0x10000 +
				Data[Index + 14] * 0x100 +
				Data[Index + 15];
			result.Scale = Data[Index + 119];
			
            // Vendor ID may not be present
            result.VendorID = System.Text.Encoding.ASCII.GetString(Data, Index+120, 8);
/*
			char[] tempArray = new char[8] {
											   (char)(Data[Index + 120]),
											   (char)(Data[Index + 121]),
											   (char)(Data[Index + 122]),
											   (char)(Data[Index + 123]),
											   (char)(Data[Index + 124]),
											   (char)(Data[Index + 125]),
											   (char)(Data[Index + 126]),
											   (char)(Data[Index + 127]) };
 */		
	
			return result;
		}

		// ---------------------------------------------------------------------------

		private static VBRData GetFhGInfo(int Index, byte[] Data)
		{
			VBRData result = new VBRData();

			// Extract FhG VBR info at given position
			result.Found = true;
			result.ID = VBR_ID_FHG.ToCharArray();
			result.Scale = Data[Index + 9];
			result.Bytes =
				Data[Index + 10] * 0x1000000 +
				Data[Index + 11] * 0x10000 +
				Data[Index + 12] * 0x100 +
				Data[Index + 13];
			result.Frames =
				Data[Index + 14] * 0x1000000 +
				Data[Index + 15] * 0x10000 +
				Data[Index + 16] * 0x100 +
				Data[Index + 17];
            result.VendorID = "";
	
			return result;
		}

		// ---------------------------------------------------------------------------

		private static VBRData FindVBR(int Index, byte[] Data) 
		{
			VBRData result;

            // Check for VBR header at given position  
            String vbrId = System.Text.Encoding.ASCII.GetString(Data, Index, 4);
			
            //char[] tempArray = new char[4] { (char)Data[Index], (char)Data[Index + 1], (char)Data[Index + 2], (char)Data[Index + 3] };

			if ( VBR_ID_XING == vbrId ) result = GetXingInfo(Index, Data);
            else if (VBR_ID_FHG == vbrId) result = GetFhGInfo(Index, Data);
            else
            {
                result = new VBRData();
                result.Reset();
            }

			return result;
		}

		// ---------------------------------------------------------------------------

		private static byte GetVBRDeviation(FrameData Frame)
		{
			// Calculate VBR deviation
			if (MPEG_VERSION_1 == Frame.VersionID)
				if (Frame.ModeID != MPEG_CM_MONO) return 36;
				else return 21;
			else
				if (Frame.ModeID != MPEG_CM_MONO) return 21;
			else return 13;
		}

		// ---------------------------------------------------------------------------

		private static FrameData FindFrame(byte[] Data, ref VBRData oVBR)
		{
			byte[] HeaderData = new byte[4];  
			FrameData result = new FrameData();
            ushort frameLength;

            result.Found = false;

			Array.Copy(Data, HeaderData, 4);

			for (int i=0; i <= Data.Length - MAX_MPEG_FRAME_LENGTH; i++)
			{
				// Decode data if frame header found
				if ( IsFrameHeader(HeaderData) )
				{
                    result.Reset();
					DecodeHeader(HeaderData, ref result);
                    frameLength = GetFrameLength(result);
					// Check for next frame and try to find VBR header
                    if (ValidFrameAt(i + frameLength, Data))
					{
                        result.Found = true;
						result.Position = i;
                        result.Size = frameLength;
						result.Xing = IsXing(i + 4, Data);
						oVBR = FindVBR(i + GetVBRDeviation(result), Data);
						break;
					}
				}
				// Prepare next data block
                Array.ConstrainedCopy(Data, i + 1, HeaderData, 0, 4);
/*
				HeaderData[0] = HeaderData[1];
				HeaderData[1] = HeaderData[2];
				HeaderData[2] = HeaderData[3];
				HeaderData[3] = Data[i + 4];
 */
			}
			return result;
		}

		// ---------------------------------------------------------------------------

		private static String FindVendorID(byte[] Data, int Size)
		{
			String VendorID;
			String result = "";
//			char[] tempArray = new char[4];

			// Search for vendor ID
			if ( (Data.Length - Size - 8) < 0 ) Size = Data.Length - 8;
			for (int i=0; i <= Size; i++)
			{
                VendorID = System.Text.Encoding.ASCII.GetString(Data, Data.Length - i - 8, 4);
/*
				tempArray[0] = (char)Data[Data.Length - i - 8];
				tempArray[1] = (char)Data[Data.Length - i - 7];
				tempArray[2] = (char)Data[Data.Length - i - 6];
				tempArray[3] = (char)Data[Data.Length - i - 5];
				VendorID = new String(tempArray);
 */
				if (VENDOR_ID_LAME == VendorID)
				{
/*
					tempArray[0] = (char)Data[Data.Length - i - 4];
					tempArray[1] = (char)Data[Data.Length - i - 3];
					tempArray[2] = (char)Data[Data.Length - i - 2];
					tempArray[3] = (char)Data[Data.Length - i - 1];
 */
                    result = VendorID + System.Text.Encoding.ASCII.GetString(Data, Data.Length - i - 4, 4);
					break;
				}
				else if (VENDOR_ID_GOGO_NEW == VendorID)
				{
					result = VendorID;
					break;
				}
			}
			return result;
		}

		// ********************** Private functions & voids *********************

		protected override void resetSpecificData()
		{
			// Reset all variables
			FVendorID = "";

            FVBR.Reset();
            FFrame.Reset();
		}

		// ---------------------------------------------------------------------------

		private String FGetVersion()
		{
			// Get MPEG version name
			return MPEG_VERSION[FFrame.VersionID];
		}

		// ---------------------------------------------------------------------------

		private String FGetLayer()
		{
			// Get MPEG layer name
			return MPEG_LAYER[FFrame.LayerID];
		}

		// ---------------------------------------------------------------------------

		private double FGetBitRate()
		{
			// Get bit rate, calculate average bit rate if VBR header found
			if ((FVBR.Found) && (FVBR.Frames > 0))
				return Math.Round(((double)FVBR.Bytes / FVBR.Frames - GetPadding(FFrame)) *
					GetSampleRate(FFrame) / GetCoefficient(FFrame));
			else
				return GetBitRate(FFrame) * 1000;
		}

		// ---------------------------------------------------------------------------

		private ushort FGetSampleRate()
		{
			// Get sample rate
			return GetSampleRate(FFrame);
		}

		// ---------------------------------------------------------------------------

		private String FGetChannelMode()
		{
			// Get channel mode name
			return MPEG_CM_MODE[FFrame.ModeID];
		}

		// ---------------------------------------------------------------------------

		private String FGetEmphasis()
		{
			// Get emphasis name
			return MPEG_EMPHASIS[FFrame.EmphasisID];
		}

		// ---------------------------------------------------------------------------

		private long FGetFrames()
		{
			long MPEGSize;

			// Get total number of frames, calculate if VBR header not found
			if (FVBR.Found) return FVBR.Frames;
			else
			{
				MPEGSize = FFileSize - FID3v2.Size - FID3v1.Size - FAPEtag.Size;
    
				return (long)Math.Floor(1.0*(MPEGSize - FFrame.Position) / GetFrameLength(FFrame));
			}
		}

		// ---------------------------------------------------------------------------

		private double FGetDuration()
		{
			// Calculate song duration
			if (FFrame.Found)
				if ((FVBR.Found) && (FVBR.Frames > 0))
					return FVBR.Frames * GetCoefficient(FFrame) * 8 / GetSampleRate(FFrame);
				else
				{
                    long MPEGSize = FFileSize - FID3v2.Size - FID3v1.Size - FAPEtag.Size;
					return (MPEGSize - FFrame.Position) / GetBitRate(FFrame) / 1000 * 8;
				}
			else
				return 0;
		}

		// ---------------------------------------------------------------------------

		private byte FGetVBREncoderID()
		{
			// Guess VBR encoder and get ID
			byte result = 0;

			if (VENDOR_ID_LAME == FVBR.VendorID.Substring(0, 4))
				result = MPEG_ENCODER_LAME;
			if (VENDOR_ID_GOGO_NEW == FVBR.VendorID.Substring(0, 4))
				result = MPEG_ENCODER_GOGO;
			if (VENDOR_ID_GOGO_OLD == FVBR.VendorID.Substring(0, 4))
				result = MPEG_ENCODER_GOGO;
			if ( StreamUtils.StringEqualsArr(VBR_ID_XING,FVBR.ID) &&
				(FVBR.VendorID.Substring(0, 4) != VENDOR_ID_LAME) &&
				(FVBR.VendorID.Substring(0, 4) != VENDOR_ID_GOGO_NEW) &&
				(FVBR.VendorID.Substring(0, 4) != VENDOR_ID_GOGO_OLD) )
				result = MPEG_ENCODER_XING;
			if ( StreamUtils.StringEqualsArr(VBR_ID_FHG,FVBR.ID))
				result = MPEG_ENCODER_FHG;

			return result;
		}

		// ---------------------------------------------------------------------------

		private byte FGetCBREncoderID()
		{
			// Guess CBR encoder and get ID
			byte result = MPEG_ENCODER_FHG;

			if ( (FFrame.OriginalBit) &&
				(FFrame.ProtectionBit) )
				result = MPEG_ENCODER_LAME;
			if ( (GetBitRate(FFrame) <= 160) &&
				(MPEG_CM_STEREO == FFrame.ModeID)) 
				result = MPEG_ENCODER_BLADE;
			if ((FFrame.CopyrightBit) &&
				(FFrame.OriginalBit) &&
				(! FFrame.ProtectionBit) )
				result = MPEG_ENCODER_XING;
			if ((FFrame.Xing) &&
				(FFrame.OriginalBit) )
				result = MPEG_ENCODER_XING;
			if (MPEG_LAYER_II == FFrame.LayerID)
				result = MPEG_ENCODER_QDESIGN;
			if ((MPEG_CM_DUAL_CHANNEL == FFrame.ModeID) &&
				(FFrame.ProtectionBit) )
				result = MPEG_ENCODER_SHINE;
			if (VENDOR_ID_LAME == FVendorID.Substring(0, 4))
				result = MPEG_ENCODER_LAME;
			if (VENDOR_ID_GOGO_NEW == FVendorID.Substring(0, 4))
				result = MPEG_ENCODER_GOGO;
		
			return result;
		}

		// ---------------------------------------------------------------------------

		private byte FGetEncoderID()
		{
			// Get guessed encoder ID
			if (FFrame.Found)
				if (FVBR.Found) return FGetVBREncoderID();
				else return FGetCBREncoderID();
			else
				return 0;
		}

		// ---------------------------------------------------------------------------

		private String FGetEncoder()
		{
			String VendorID = "";
			String result;

			// Get guessed encoder name and encoder version for LAME
			result = MPEG_ENCODER[FGetEncoderID()];
			if (FVBR.VendorID != "") VendorID = FVBR.VendorID;
			if (FVendorID != "") VendorID = FVendorID;
			if ( (MPEG_ENCODER_LAME == FGetEncoderID()) &&
				(VendorID.Length >= 8) &&
				Char.IsDigit(VendorID[4]) &&
				(VendorID[5] == '.') &&
				(Char.IsDigit(VendorID[6]) &&
				Char.IsDigit(VendorID[7]) ))
				result =
					result + (char)32 +
					VendorID[4] +
					VendorID[5] +
					VendorID[6] +
					VendorID[7];
			return result;
		}

		// ---------------------------------------------------------------------------

		private bool FGetValid()
		{
			// Check for right MPEG file data
			return
				((FFrame.Found) &&
				(FGetBitRate() >= MIN_MPEG_BIT_RATE) &&
				(FGetBitRate() <= MAX_MPEG_BIT_RATE) &&
				(FGetDuration() >= MIN_ALLOWED_DURATION));
		}

		// ********************** Public functions & voids **********************

		public TMPEGaudio()
		{
			// Object constructor  
			FID3v1 = new TID3v1();
			FID3v2 = new TID3v2();
			FAPEtag = new TAPEtag();
			resetData();
		}

		// ---------------------------------------------------------------------------

		//No explicit destructors in C#

		// ---------------------------------------------------------------------------

        public override bool Read(BinaryReader source, StreamUtils.StreamHandlerDelegate pictureStreamHandler)
        {
            Stream fs = source.BaseStream;
			byte[] Data = new byte[MAX_MPEG_FRAME_LENGTH * 2];  

			bool result = false;

			// At first search for tags, then search for a MPEG frame and VBR data
            FID3v2.Read(source, pictureStreamHandler);
            FID3v1.Read(source);
            APEtag.Read(source, pictureStreamHandler);

			fs.Seek(FID3v2.Size, SeekOrigin.Begin);
			Data = source.ReadBytes(Data.Length);
			FFrame = FindFrame(Data, ref FVBR);
		  
			// Try to search in the middle if no frame at the beginning found
			if ( ! FFrame.Found ) 
			{
				fs.Seek((long)Math.Floor((FFileSize - FID3v2.Size) / 2.0),SeekOrigin.Begin);
				Data = source.ReadBytes(Data.Length);
				FFrame = FindFrame(Data, ref FVBR);
			}
			// Search for vendor ID at the end if CBR encoded
			if ( (FFrame.Found) && (! FVBR.Found) )
			{
                fs.Seek(FFileSize - Data.Length - FID3v1.Size - FAPEtag.Size, SeekOrigin.Begin);
				Data = source.ReadBytes(Data.Length);
				FVendorID = FindVendorID(Data, FFrame.Size * 5);
			}
			result = true;

            FValid = FFrame.Found;

            if (!FValid)
            {
                resetData();
            }
            else
            {
                FBitrate = FGetBitRate();
                FDuration = FGetDuration();
            }
	
			return result;
		}

	}
}