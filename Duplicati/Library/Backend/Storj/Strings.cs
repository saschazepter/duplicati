// Copyright (C) 2025, The Duplicati Team
// https://duplicati.com, hello@duplicati.com
// 
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"), 
// to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, 
// and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS 
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
using Duplicati.Library.Localization.Short;

namespace Duplicati.Library.Backend.Strings
{
    internal static class Storj
    {
        public static string Description => LC.L(@"This backend can read and write data to the Storj DCS.");
        public static string DisplayName => LC.L(@"Storj DCS (Decentralized Cloud Storage)");
        public static string StorjAuthMethodDescriptionLong => LC.L(@"Specify the authentication method which describes which way to use to connect to the network - either via API key or via an access grant.");
        public static string StorjAuthMethodDescriptionShort => LC.L(@"Authentication method");
        public static string StorjSatelliteDescriptionLong => LC.L(@"Specify the satellite that keeps track of all metadata. Use a Storj DCS server for high-performance SLA-backed connectivity or use a community server. Or even host your own.");
        public static string StorjSatelliteDescriptionShort => LC.L(@"Satellite");
        public static string StorjAPIKeyDescriptionLong => LC.L(@"Supply the API key which grants access to a specific project on your chosen satellite. Head over to the dashboard of your satellite to create one if you do not already have an API key.");
        public static string StorjAPIKeyDescriptionShort => LC.L(@"API key");
        public static string StorjSecretDescriptionLong => LC.L(@"Supply the encryption passphrase used to encrypt your data before sending it to the Storj network. This passphrase can be the only secret to provide - for Storj you do not necessary need any additional encryption (from Duplicati) in place.");
        public static string StorjSecretDescriptionShort => LC.L(@"Encryption passphrase");
        public static string StorjSharedAccessDescriptionLong => LC.L(@"Supply the access grant which contains all information in one encrypted string. You may use it instead of a satellite, API key and secret.");
        public static string StorjSharedAccessDescriptionShort => LC.L(@"Access grant");
        public static string StorjBucketDescriptionLong => LC.L(@"Specify the bucket for storing the backup.");
        public static string StorjBucketDescriptionShort => LC.L(@"Bucket");
        public static string StorjFolderDescriptionLong => LC.L(@"Specify the folder in the bucket for storing the backup.");
        public static string StorjFolderDescriptionShort => LC.L(@"Folder");
    }

    internal static class StorjConfig
    {
        public static string DisplayName => LC.L("Storj DCS configuration module");
        public static string Description => LC.L("Expose Storj DCS configuration as a web module");
    }
}
