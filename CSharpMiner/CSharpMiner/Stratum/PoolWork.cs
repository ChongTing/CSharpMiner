﻿using CSharpMiner.Helpers;
using System;
using System.Security.Cryptography;
using System.Linq;

namespace CSharpMiner.Stratum
{
    public class PoolWork
    {
        Object _lock = new Object();

        public Object[] CommandArray { get; private set; }

        public string JobId { get; private set; }
        public string PreviousHash { get; private set; }
        public string Coinbase1 { get; private set; }
        public string Coinbase2 { get; private set; }
        public string[] MerkleBranch { get; private set; }
        public string Version { get; private set; }
        public string NetworkDiff { get; private set; } // nbits
        public int Diff { get; private set; }
        public string Timestamp { get; private set; }
        public string Extranonce1 { get; private set; }
        public int StartingNonce { get; private set; }

        private string _extranonce2 = null;
        public string Extranonce2 
        { 
            get
            {
                return _extranonce2;
            }

            set
            {
                _merkleRoot = null; // Clear out the old value since it is invalid
                _header = null; // Clear out the old value since it is invalid
                _extranonce2 = value;
            }
        }

        private string _merkleRoot = null;
        public string MerkleRoot
        {
            get
            {
                if(_merkleRoot == null)
                {
                    lock (_lock)
                    {
                        if (_merkleRoot == null) // Just in case a thread was waiting on the lock. No sense in recomputing the merkleRoot
                        {
                            _merkleRoot = ComputeMerkleRoot();
                        }
                    }
                }

                return _merkleRoot;
            }
        }

        private string _header = null;
        public string Header
        {
            get
            {
                if (_header == null)
                {
                    _header = MakeHeader();
                }

                return _header;
            }
        }

        private static SHA256 _sha256 = null;
        public static SHA256 SHA256Hash
        {
            get
            {
                if(_sha256 == null)
                {
                    _sha256 = SHA256.Create();
                }

                return _sha256;
            }
        }

        public PoolWork(Object[] serverCommandArray, string extranonce1, string extranonce2, int diff, int startingNonce = 0)
        {
            if(serverCommandArray.Length < 8)
            {
                throw new ArgumentException("Unrecognized work format from server. Work array length < 8.");
            }

            CommandArray = serverCommandArray;

            Extranonce1 = extranonce1;
            Extranonce2 = extranonce2;
            Diff = diff;

            StartingNonce = startingNonce;

            JobId = serverCommandArray[0] as string;
            PreviousHash = serverCommandArray[1] as string;
            Coinbase1 = serverCommandArray[2] as string;
            Coinbase2 = serverCommandArray[3] as string;
            Version = serverCommandArray[5] as string;
            NetworkDiff = serverCommandArray[6] as string;
            Timestamp = serverCommandArray[7] as string;

            Object[] merkleTreeParts = serverCommandArray[4] as Object[];

            if(merkleTreeParts != null)
            {
                MerkleBranch = new string[merkleTreeParts.Length];

                for(int i = 0; i < merkleTreeParts.Length; i++)
                {
                    MerkleBranch[i] = merkleTreeParts[i] as string;
                }
            }
            else
            {
                throw new ArgumentException("Unrecognized work format from server. Merkle_Branch is not an array.");
            }

            LogHelper.DebugConsoleLogAsync(new Object[] {
                new Object[] { "Work:"},
                new Object[] { string.Format("nonce1: {0}", Extranonce1)},
                new Object[] { string.Format("nonce2: {0}", Extranonce2)},
                new Object[] { string.Format("diff: {0}", Diff)},
                new Object[] { string.Format("id: {0}", JobId)},
                new Object[] { string.Format("prevHash: {0}", PreviousHash)},
                new Object[] { string.Format("coinb1: {0}", Coinbase1)},
                new Object[] { string.Format("coinb2: {0}", Coinbase2)},
                new Object[] { string.Format("version: {0}", Version)},
                new Object[] { string.Format("nbits: {0}", NetworkDiff)},
                new Object[] { string.Format("ntime: {0}", Timestamp)}
            });
        }

        private string ComputeMerkleRoot()
        {
            string coinbase = string.Format("{0}{1}{2}{3}", Coinbase1, Extranonce1, Extranonce2, Coinbase2);
            byte[] coinbaseBinary = HexConversionHelper.ConvertFromHexString(coinbase);

            SHA256 sha256 = SHA256Hash;
            byte[] merkleRoot = sha256.ComputeHash(sha256.ComputeHash(coinbaseBinary));

            foreach (string str in MerkleBranch)
            {
                merkleRoot = sha256.ComputeHash(sha256.ComputeHash(merkleRoot.Concat(HexConversionHelper.ConvertFromHexString(str)).ToArray()));
            }

            return HexConversionHelper.Swap(HexConversionHelper.ConvertToHexString(merkleRoot));
        }

        private string MakeHeader()
        {
            return string.Format("{0}{1}{2}{3}{4}", Version, PreviousHash, MerkleRoot, Timestamp, NetworkDiff);
        }
    }
}
