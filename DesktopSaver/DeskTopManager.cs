using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Runtime.Serialization;

namespace DesktopSaver {

    #region enums
    public enum ShellChangeNotifyEvent : uint {
        SHCNE_RENAMEITEM = 0x00000001,
        SHCNE_CREATE = 0x00000002,
        SHCNE_DELETE = 0x00000004,
        SHCNE_MKDIR = 0x00000008,
        SHCNE_RMDIR = 0x00000010,
        SHCNE_MEDIAINSERTED = 0x00000020,
        SHCNE_MEDIAREMOVED = 0x00000040,
        SHCNE_DRIVEREMOVED = 0x00000080,
        SHCNE_DRIVEADD = 0x00000100,
        SHCNE_NETSHARE = 0x00000200,
        SHCNE_NETUNSHARE = 0x00000400,
        SHCNE_ATTRIBUTES = 0x00000800,
        SHCNE_UPDATEDIR = 0x00001000,
        SHCNE_UPDATEITEM = 0x00002000,
        SHCNE_SERVERDISCONNECT = 0x00004000,
        SHCNE_UPDATEIMAGE = 0x00008000,
        SHCNE_DRIVEADDGUI = 0x00010000,
        SHCNE_RENAMEFOLDER = 0x00020000,
        SHCNE_FREESPACE = 0x00040000,
        SHCNE_EXTENDED_EVENT = 0x04000000,
        SHCNE_ASSOCCHANGED = 0x08000000,
        SHCNE_DISKEVENTS = 0x0002381F,
        SHCNE_GLOBALEVENTS = 0x0C0581E0,
        SHCNE_ALLEVENTS = 0x7FFFFFFF,
        SHCNE_INTERRUPT = 0x80000000,
    }

    public enum ShellChangeNotifyFlag : uint {
        SHCNF_IDLIST = 0x0000,
        SHCNF_PATHA = 0x0001,
        SHCNF_PRINTERA = 0x0002,
        SHCNF_DWORD = 0x0003,
        SHCNF_PATHW = 0x0005,
        SHCNF_PRINTERW = 0x0006,
        SHCNF_TYPE = 0x00FF,
        SHCNF_FLUSH = 0x1000,
        SHCNF_FLUSHNOWAIT = 0x2000
    }

    public enum ProcessAllocationType : uint {
        MEM_COMMIT = 0x1000,
        MEM_RELEASE = 0x8000,
        MEM_RESERVE = 0x2000
    }

    public enum ProcessMemoryProtection : uint {
        PAGE_READWRITE = 4
    }

    public enum ProcessMemoryOperation : uint {
        PROCESS_VM_OPERATION = 0x0008,
        PROCESS_VM_READ = 0x0010,
        PROCESS_VM_WRITE = 0x0020
    }
     
    public enum ListViewMessage : uint {
        LVM_FIRST = 0x1000,
        LVM_GETITEMCOUNT = LVM_FIRST + 4,
        LVM_GETITEMW = LVM_FIRST + 75,
        LVM_SETITEMPOSITION = LVM_FIRST + 15,
        LVM_GETITEMPOSITION = LVM_FIRST + 16,
    }
    #endregion

    public class DeskTopManager {

        private const uint PAGE_READWRITE = 4;

        private const int LVIF_TEXT = 0x0001;

        private const int READ_BUFFER_SIZE = 256;

        #region DllImports
        [DllImport( "kernel32.dll" )]
        private static extern IntPtr VirtualAllocEx( IntPtr hProcess, IntPtr lpAddress, uint dwSize, ProcessAllocationType flAllocationType, ProcessMemoryProtection flProtect );

        [DllImport( "kernel32.dll" )]
        private static extern bool VirtualFreeEx( IntPtr hProcess, IntPtr lpAddress, uint dwSize, ProcessAllocationType dwFreeType );

        [DllImport( "kernel32.dll" )]
        private static extern bool CloseHandle( IntPtr handle );

        [DllImport( "kernel32.dll" )]
        private static extern bool WriteProcessMemory( IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int nSize, ref uint vNumberOfBytesRead );

        [DllImport( "kernel32.dll" )]
        private static extern bool ReadProcessMemory( IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int nSize, ref uint vNumberOfBytesRead );

        [DllImport( "kernel32.dll" )]
        private static extern IntPtr OpenProcess( ProcessMemoryOperation dwDesiredAccess, bool bInheritHandle, uint dwProcessId );

        [DllImport( "user32.DLL" )]
        private static extern int SendMessage( IntPtr hWnd, ListViewMessage Msg, IntPtr wParam, IntPtr lParam );

        [DllImport( "user32.DLL" )]
        private static extern IntPtr FindWindow( string lpszClass, string lpszWindow );

        [DllImport( "user32.DLL" )]
        private static extern IntPtr FindWindowEx( IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow );

        [DllImport( "user32.dll" )]
        private static extern uint GetWindowThreadProcessId( IntPtr hWnd, out uint dwProcessId );

        [DllImport( "Shell32.dll" )]
        private static extern int SHChangeNotify( ShellChangeNotifyEvent eventId, ShellChangeNotifyFlag flags, IntPtr item1, IntPtr item2 );
        #endregion

        #region struct
        private struct LVITEM {
            public int mask;
            public int iItem;
            public int iSubItem;
            public int state;
            public int stateMask;
            public IntPtr pszText; // string
            public int cchTextMax;
            public int iImage;
            public IntPtr lParam;
            public int iIndent;
            public int iGroupId;
            public int cColumns;
            public IntPtr puColumns;
        }
        #endregion



        public static event EventHandler<MessageEventArgs> ErrorMessage;

        private static void RaiseErrorMessage( Exception e, string message ) {
            try {
                EventHandler<MessageEventArgs> evt = ErrorMessage;

                if ( null != evt ) {
                    evt( null, new MessageEventArgs( e, message ) );
                }
            } catch ( Exception ) {

            }
        }



        private static IntPtr GetDesktopListView() {
            var hwnd = FindWindow( "Progman", null );
            var shHwnd = FindWindowEx( hwnd, IntPtr.Zero, "SHELLDLL_DefView", null );
            return FindWindowEx( shHwnd, IntPtr.Zero, "SysListView32", null );
        }

        private static uint GetDesktopProcess() {
            var hwnd = GetDesktopListView();

            uint id;
            GetWindowThreadProcessId( hwnd, out id );
            return id;
        }


        public static List<DesktopIcon> GetDesktopIcons() {
            List<DesktopIcon> _return = new List<DesktopIcon>();

            RaiseErrorMessage( null, string.Format( "Begin GetDesktopIcons" ) );

            var lvHwnd = GetDesktopListView();

            int iconCount = SendMessage( lvHwnd, ListViewMessage.LVM_GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero );

            uint lvProcessId;
            GetWindowThreadProcessId( lvHwnd, out lvProcessId );

            IntPtr lvProcess = OpenProcess( ProcessMemoryOperation.PROCESS_VM_OPERATION | ProcessMemoryOperation.PROCESS_VM_READ | ProcessMemoryOperation.PROCESS_VM_WRITE, false, lvProcessId );
            IntPtr pointer = VirtualAllocEx( lvProcess, IntPtr.Zero, 4096, ProcessAllocationType.MEM_RESERVE | ProcessAllocationType.MEM_COMMIT, ProcessMemoryProtection.PAGE_READWRITE );

            try {
                for ( int index = 0; index < iconCount; index++ ) {
                    try {
                        byte[] buffer = new byte[READ_BUFFER_SIZE];
                        LVITEM item = new LVITEM() {
                            mask = LVIF_TEXT,
                            iItem = index,
                            iSubItem = 0,
                            cchTextMax = buffer.Length,
                            pszText = ( IntPtr ) ( ( int ) pointer + Marshal.SizeOf( typeof( LVITEM ) ) )
                        };
                        LVITEM[] items = new LVITEM[] { item };

                        uint read = 0;

                        WriteProcessMemory( lvProcess, pointer, Marshal.UnsafeAddrOfPinnedArrayElement( items, 0 ), Marshal.SizeOf( typeof( LVITEM ) ), ref read );
                        SendMessage( lvHwnd, ListViewMessage.LVM_GETITEMW, ( IntPtr ) index, pointer );
                        ReadProcessMemory( lvProcess, ( IntPtr ) ( ( int ) pointer + Marshal.SizeOf( typeof( LVITEM ) ) ), Marshal.UnsafeAddrOfPinnedArrayElement( buffer, 0 ), buffer.Length, ref read );
                        item.pszText = ( IntPtr ) ( buffer.Length + Marshal.SizeOf( typeof( LVITEM ) ) );

                        var text = Encoding.Unicode.GetString( buffer, 0, ( int ) read ).Trim( '\0' );

                        if ( text.Contains( '\0' ) ) {
                            text = text.Substring( 0, ( text.IndexOf( '\0' ) ) );
                        }

                        SendMessage( lvHwnd, ListViewMessage.LVM_GETITEMPOSITION, ( IntPtr ) index, pointer );
                        Point point = new Point();
                        Point[] points = new Point[] { point };
                        ReadProcessMemory( lvProcess, pointer, Marshal.UnsafeAddrOfPinnedArrayElement( points, 0 ), Marshal.SizeOf( typeof( Point ) ), ref read );
                        var icon = new DesktopIcon( index, text, points[0] );
                        Trace.WriteLine( icon );
                        _return.Add( icon );
                    } catch ( Exception e ) {
                        RaiseErrorMessage( e, string.Format( "Error in GetDesktopIcons with index ({0}) - {1}", index, e.Message ) );
                    }
                }
            } catch ( Exception ex ) {
                RaiseErrorMessage( ex, string.Format( "Error in GetDesktopIcons - {0}", ex.Message ) );
            } finally {
                VirtualFreeEx( lvProcess, pointer, 0, ProcessAllocationType.MEM_RELEASE );
                CloseHandle( lvProcess );
            }

            RaiseErrorMessage( null, string.Format( "Completed GetDesktopIcons") );

            return _return;
        }

        public static void SetDesktopIcons( List<DesktopIcon> icons ) {
            RaiseErrorMessage( null, string.Format( "Begin SetDesktopIcons" ) );
            // for each icon...find the source desktop icon...and update position if found...
            var lvHwnd = GetDesktopListView();

            int iconCount = SendMessage( lvHwnd, ListViewMessage.LVM_GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero );

            uint lvProcessId;
            GetWindowThreadProcessId( lvHwnd, out lvProcessId );

            IntPtr lvProcess = OpenProcess( ProcessMemoryOperation.PROCESS_VM_OPERATION | ProcessMemoryOperation.PROCESS_VM_READ | ProcessMemoryOperation.PROCESS_VM_WRITE, false, lvProcessId );
            IntPtr pointer = VirtualAllocEx( lvProcess, IntPtr.Zero, 4096, ProcessAllocationType.MEM_RESERVE | ProcessAllocationType.MEM_COMMIT, ProcessMemoryProtection.PAGE_READWRITE );

            try {
                for ( int index = 0; index < iconCount; index++ ) {
                    try {
                        byte[] buffer = new byte[256];
                        LVITEM item = new LVITEM() {
                            mask = LVIF_TEXT,
                            iItem = index,
                            iSubItem = 0,
                            cchTextMax = buffer.Length,
                            pszText = ( IntPtr ) ( ( int ) pointer + Marshal.SizeOf( typeof( LVITEM ) ) )
                        };
                        LVITEM[] items = new LVITEM[] { item };

                        uint read = 0;

                        WriteProcessMemory( lvProcess, pointer, Marshal.UnsafeAddrOfPinnedArrayElement( items, 0 ), Marshal.SizeOf( typeof( LVITEM ) ), ref read );
                        SendMessage( lvHwnd, ListViewMessage.LVM_GETITEMW, ( IntPtr ) index, pointer );
                        ReadProcessMemory( lvProcess, ( IntPtr ) ( ( int ) pointer + Marshal.SizeOf( typeof( LVITEM ) ) ), Marshal.UnsafeAddrOfPinnedArrayElement( buffer, 0 ), buffer.Length, ref read );
                        item.pszText = ( IntPtr ) ( buffer.Length + Marshal.SizeOf( typeof( LVITEM ) ) );

                        var text = Encoding.Unicode.GetString( buffer, 0, ( int ) read ).Trim( '\0' );

                        if ( text.Contains( '\0' ) ) {
                            text = text.Substring( 0, ( text.IndexOf( '\0' ) ) );
                        }

                        RaiseErrorMessage( null, string.Format( "In SetDesktopIcons placing index ({0}) - {1}", index, text) );

                        // look through list to find this icon, based on text???
                        var icon = icons.FirstOrDefault( i => i.Text == text && i.Index == index );

                        if ( icon != null ) {
                            SendMessage( lvHwnd, ListViewMessage.LVM_SETITEMPOSITION, ( IntPtr ) index, MakeLParam( icon.Point.X, icon.Point.Y ) );
                        } else {
                            throw new Exception(string.Format( "SetDesktopIcons index ({0}) - {1} ...NOT FOUND", index, text ) );
                        }
                    } catch ( Exception e ) {
                        RaiseErrorMessage( e, string.Format( "Error in SetDesktopIcons with index ({0}) - {1}", index, e.Message ) );
                    }
                }   // for each desktop icon count

                SHChangeNotify( ShellChangeNotifyEvent.SHCNE_UPDATEITEM, ShellChangeNotifyFlag.SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero );
            } catch ( Exception ex ) {
                RaiseErrorMessage( ex, string.Format( "Error in GetDesktopIcons - {0}", ex.Message ) );
            } finally {
                VirtualFreeEx( lvProcess, pointer, 0, ProcessAllocationType.MEM_RELEASE );
                CloseHandle( lvProcess );
            }

            RaiseErrorMessage( null, string.Format( "Completed SetDesktopIcons" ) );
        }







        private static DesktopIcon GetDesktopIcon( int index ) {
            DesktopIcon _return = null;

            var desktop = ( IntPtr ) GetDesktopProcess();

            IntPtr lviItemMem = VirtualAllocEx( desktop, IntPtr.Zero, 4096, ProcessAllocationType.MEM_RESERVE | ProcessAllocationType.MEM_COMMIT, ProcessMemoryProtection.PAGE_READWRITE );
            IntPtr lviItemText = VirtualAllocEx( desktop, IntPtr.Zero, 4096, ProcessAllocationType.MEM_RESERVE | ProcessAllocationType.MEM_COMMIT, ProcessMemoryProtection.PAGE_READWRITE );

            try {
                uint read = 0;

                LVITEM item = new LVITEM() {
                    mask = LVIF_TEXT,
                    iItem = index,
                    iSubItem = 0,
                    cchTextMax = READ_BUFFER_SIZE,
                    pszText = lviItemText
                };

                IntPtr itemPointer = Marshal.AllocHGlobal( Marshal.SizeOf( item ) );
                Marshal.StructureToPtr( item, itemPointer, false );

                WriteProcessMemory( desktop, lviItemMem, itemPointer, Marshal.SizeOf( item ), ref read );
                Marshal.FreeHGlobal( itemPointer );

                SendMessage( desktop, ListViewMessage.LVM_GETITEMW, ( IntPtr ) index, lviItemMem );

                byte[] buffer = new byte[READ_BUFFER_SIZE];
                ReadProcessMemory( desktop, lviItemText, Marshal.UnsafeAddrOfPinnedArrayElement( buffer, 0 ), READ_BUFFER_SIZE, ref read );

                item.pszText = ( IntPtr ) ( buffer.Length + Marshal.SizeOf( typeof( LVITEM ) ) );

                var text = Encoding.Unicode.GetString( buffer, 0, ( int ) read );

                if ( text.Contains( '\0' ) ) {
                    text = text.Substring( 0, ( text.IndexOf( '\0' ) ) );
                }

                SendMessage( desktop, ListViewMessage.LVM_GETITEMPOSITION, ( IntPtr ) index, lviItemMem );
                Point point = new Point();
                Point[] points = new Point[] { point };
                ReadProcessMemory( desktop, lviItemMem, Marshal.UnsafeAddrOfPinnedArrayElement( points, 0 ), Marshal.SizeOf( typeof( Point ) ), ref read );

                _return = new DesktopIcon( index, text, point );
            } catch ( Exception e ) {
                Trace.WriteLine( string.Format( "ERROR(GetIconText): {0}", e.Message ) );
            } finally {
                if ( lviItemMem != IntPtr.Zero )
                    VirtualFreeEx( desktop, lviItemMem, 0, ProcessAllocationType.MEM_RELEASE );

                if ( lviItemText != IntPtr.Zero )
                    VirtualFreeEx( desktop, lviItemText, 0, ProcessAllocationType.MEM_RELEASE );

            }

            return _return;
        }

        private static IntPtr MakeLParam( int low, int high ) {
            return ( IntPtr ) ( ( ( short ) high << 16 ) | ( low & 0xffff ) );
        }

    }
     
    public class MessageEventArgs : EventArgs {
        private Exception error;
        private string message;

        public MessageEventArgs( Exception ex, string message ) {
            this.error = ex;
            this.message = message;
        }

        public Exception Error {
            get { return this.error; }
        }

        public string Message {
            get { return this.message; }
        }
    }

    [DataContract]
    public class DesktopIcon {

        public DesktopIcon() {

        }

        public DesktopIcon( int index, string text, Point point )
            : this() {
            Index = index;
            Text = text;
            Point = point;
        }

        [DataMember( Name = "Index" )]
        public int Index {
            get;
            set;
        }

        [DataMember( Name = "Text" )]
        public string Text {
            get;
            set;
        }

        [DataMember( Name = "Point" )]
        public Point Point {
            get;
            set;
        }

        public override string ToString() {
            return string.Format( "[{3,-4}] {0,-35} => X:{1}  Y:{2}", Text, Point.X, Point.Y, Index );
        }
    }

}