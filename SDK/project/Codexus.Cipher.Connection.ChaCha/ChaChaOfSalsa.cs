using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;

namespace Codexus.Cipher.Connection.ChaCha;

public sealed class ChaChaOfSalsa : ChaCha7539Engine
{
	private readonly int _rounds;

	public ChaChaOfSalsa(byte[] key, byte[] iv, bool encryption, int rounds = 8)
	{
		_rounds = rounds;
		Init(encryption, new ParametersWithIV(new KeyParameter(key), iv));
	}

	public override string AlgorithmName => $"ChaCha{_rounds}";
}
