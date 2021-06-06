using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

// based on https://github.com/lithander/Minimal-Bitcoin-Miner
namespace MiniMiner
{
	class Work
	{
		public Work(byte[] data, byte[] target, uint nonceStart, uint increment, uint batchSize)
		{
			Data = data;
			Target = target;
			Current = (byte[])data.Clone();
			CurrentNonce = nonceStart;
			_nonceOffset = Data.Length - 4;
			_hasher = new SHA256Managed();
			_increment = increment;
			_batchSize = batchSize;
		}

		private SHA256Managed _hasher;
        private uint _increment;
        private uint _batchSize;
        private long _nonceOffset;
		public byte[] Data;
		public byte[] Target;
		public byte[] Current;
		public uint CurrentNonce;
		public byte[] CurrentHash = null;

		internal bool WorkBatch()
		{
			for (var n = 0; n < _batchSize; n++)
			{
				CurrentNonce += _increment;

				BitConverter.GetBytes(CurrentNonce).CopyTo(Current, _nonceOffset);
				CurrentHash = Sha256(Sha256(Current));

				// possible candidate, ends with zeroes, check further
				if (CurrentHash[31] == 0 && CurrentHash[30] == 0 && CurrentHash[29] == 0 && CurrentHash[28] == 0)
					if(meetsTarget(CurrentHash, Target)) 
						return true;
			}
			return false;
		}

		private byte[] Sha256(byte[] input)
		{
			byte[] crypto = _hasher.ComputeHash(input, 0, input.Length);
			return crypto;
		}

		public bool meetsTarget(byte[] hash, byte[] target)
		{
			for (int i = hash.Length - 1; i >= 0; i--)
			{
				if ((hash[i] & 0xff) > (target[i] & 0xff))
					return false;
				if ((hash[i] & 0xff) < (target[i] & 0xff))
					return true;
			}
			return false;
		}
	}
}
