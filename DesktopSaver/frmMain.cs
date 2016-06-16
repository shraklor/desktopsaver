using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace DesktopSaver {
    public partial class frmMain : Form {
        private readonly string FILE = string.Format( "{0}.sav", Application.ProductName );

        public frmMain() {
            InitializeComponent();
        }

        private void frmMain_Load( object sender, EventArgs e ) {
        }

        private void cmdSave_Click( object sender, EventArgs e ) {
            var icons = DeskTopManager.GetDesktopIcons();

            // serialize them and save to file in current directory
            SerializeObject<List<DesktopIcon>>( icons, GetWorkingFile() );
        }

        private void cmdRestore_Click( object sender, EventArgs e ) {
            // read from file in current directory, and de-serialize contents
            List<DesktopIcon> icons = DeSerializeObject<List<DesktopIcon>>( GetWorkingFile() );



            foreach ( var icon in icons ) {
                Trace.WriteLine( icon );
            }

            DeskTopManager.SetDesktopIcons( icons );
        }

        private string GetWorkingFile() {
            var here = Directory.GetCurrentDirectory();

            return Path.Combine( here, FILE );
        }

        public void SerializeObject<T>( T serializableObject, string fileName ) {
            if ( serializableObject == null )
                return;

            try {
                XmlDocument doc = new XmlDocument();
                XmlSerializer serializer = new XmlSerializer( serializableObject.GetType() );
                using ( MemoryStream stream = new MemoryStream() ) {
                    serializer.Serialize( stream, serializableObject );
                    stream.Position = 0;
                    doc.Load( stream );
                    doc.Save( fileName );
                    stream.Close();
                }
            } catch ( Exception e ) {
                Trace.WriteLine( string.Format( "Serialization error: " + e.Message ) );
            }
        }

        public T DeSerializeObject<T>( string fileName ) {
            if ( string.IsNullOrEmpty( fileName ) )
                return default( T );

            T _return = default( T );

            try {
                XmlDocument doc = new XmlDocument();
                doc.Load( fileName );
                string xmlString = doc.OuterXml;

                using ( StringReader read = new StringReader( xmlString ) ) {
                    Type type = typeof( T );

                    XmlSerializer serializer = new XmlSerializer( type );
                    using ( XmlReader reader = new XmlTextReader( read ) ) {
                        _return = ( T ) serializer.Deserialize( reader );
                        reader.Close();
                    }

                    read.Close();
                }
            } catch ( Exception e ) {
                Trace.WriteLine( string.Format( "Deserialization error: " + e.Message ) );
            }

            return _return;
        }

        private void saveToolStripMenuItem_Click( object sender, EventArgs e ) {
            cmdSave.PerformClick();
        }

        private void restoreToolStripMenuItem_Click( object sender, EventArgs e ) {
            cmdRestore.PerformClick();
        }

        private void frmMain_Load_1( object sender, EventArgs e ) {

        }

    }

}