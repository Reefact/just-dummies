// Scaffolded by `any` (JustDummies), then rewritten by hand: three parameters are tied to the token's cap
// (initial supply, minimum gas price) and to the schedule, so they are drawn after them in Generate().

using System;
using System.Numerics;

using JustDummies;

namespace TokenLedger.Domain;

/// <summary>A generator of arbitrary <see cref="Ledger" /> values.</summary>
public sealed partial class AnyLedger : IAny<Ledger> {

    private IAny<TokenDefinition> _token;
    private IAny<Address>         _minter;
    private IAny<Address>         _feeCollector;
    private IAny<GasSchedule>     _gasSchedule;
    private IAny<ulong>           _genesisBlock;
    private TokenAmount?          _initialSupply;
    private TokenAmount?          _minimumGasPrice;

    private AnyTokenDefinition _tokenRecipe;
    private AnyGasSchedule     _gasScheduleRecipe;

    public AnyLedger()
        : this(token: new AnyTokenDefinition(), gasSchedule: new AnyGasSchedule()) { }

    private AnyLedger(AnyTokenDefinition token, AnyGasSchedule gasSchedule) {
        _token             = token;
        _tokenRecipe       = token;
        _gasSchedule       = gasSchedule;
        _gasScheduleRecipe = gasSchedule;
        _minter            = new AnyAddress();
        _feeCollector      = new AnyAddress();
        _genesisBlock      = Any.UInt64();
    }

    private AnyLedger(AnyLedger from) {
        _token             = from._token;
        _tokenRecipe       = from._tokenRecipe;
        _minter            = from._minter;
        _feeCollector      = from._feeCollector;
        _gasSchedule       = from._gasSchedule;
        _gasScheduleRecipe = from._gasScheduleRecipe;
        _genesisBlock      = from._genesisBlock;
        _initialSupply     = from._initialSupply;
        _minimumGasPrice   = from._minimumGasPrice;
    }

    public AnyLedger WithToken(TokenDefinition value) {
        AnyLedger copy = new AnyLedger(this);

        copy._token = Any.Fixed(value);

        return copy;
    }

    public AnyLedger WithToken(Func<AnyTokenDefinition, AnyTokenDefinition> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLedger copy = new AnyLedger(this);

        copy._tokenRecipe = configure(_tokenRecipe);
        copy._token       = copy._tokenRecipe;

        return copy;
    }

    public AnyLedger WithMinter(Address value) {
        AnyLedger copy = new AnyLedger(this);

        copy._minter = Any.Fixed(value);

        return copy;
    }

    public AnyLedger WithFeeCollector(Address value) {
        AnyLedger copy = new AnyLedger(this);

        copy._feeCollector = Any.Fixed(value);

        return copy;
    }

    public AnyLedger WithInitialSupply(TokenAmount value) {
        AnyLedger copy = new AnyLedger(this);

        copy._initialSupply = value;

        return copy;
    }

    public AnyLedger WithGasSchedule(GasSchedule value) {
        AnyLedger copy = new AnyLedger(this);

        copy._gasSchedule = Any.Fixed(value);

        return copy;
    }

    public AnyLedger WithGasSchedule(Func<AnyGasSchedule, AnyGasSchedule> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLedger copy = new AnyLedger(this);

        copy._gasScheduleRecipe = configure(_gasScheduleRecipe);
        copy._gasSchedule       = copy._gasScheduleRecipe;

        return copy;
    }

    public AnyLedger WithMinimumGasPrice(TokenAmount value) {
        AnyLedger copy = new AnyLedger(this);

        copy._minimumGasPrice = value;

        return copy;
    }

    public AnyLedger WithGenesisBlock(ulong value) {
        AnyLedger copy = new AnyLedger(this);

        copy._genesisBlock = Any.Fixed(value);

        return copy;
    }

    public Ledger Generate() {
        TokenDefinition token       = _token.Generate();
        GasSchedule     gasSchedule = _gasSchedule.Generate();
        BigInteger      cap         = token.SupplyCap.BaseUnits;

        // Neither the initial supply nor the minimum price may pass what the cap allows.
        TokenAmount initialSupply   = _initialSupply ?? new AnyTokenAmount().WithBaseUnits(b => b.Between(BigInteger.Zero, cap)).Generate();
        TokenAmount minimumGasPrice = _minimumGasPrice ?? new AnyTokenAmount().WithBaseUnits(b => b.Between(BigInteger.Zero, cap / gasSchedule.Transfer)).Generate();

        return new Ledger(token, _minter.Generate(), _feeCollector.Generate(), initialSupply, gasSchedule, minimumGasPrice, _genesisBlock.Generate());
    }

}
