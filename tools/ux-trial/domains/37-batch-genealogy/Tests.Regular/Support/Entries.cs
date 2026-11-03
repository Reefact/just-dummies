namespace BatchGenealogy.Domain;

/// <summary>Entry points: <c>Any.Lot()</c> reads like <c>Any.Int32()</c>. Written by hand, since the scaffold was edited before an entry point was wanted.</summary>
public static class AnyEntry {

    extension(Any) {

        public static AnyLot             Lot()             => new AnyLot();
        public static AnyLotNumber       LotNumber()       => new AnyLotNumber();
        public static AnyMaterialCode    MaterialCode()    => new AnyMaterialCode();
        public static AnyQuantity        Quantity()        => new AnyQuantity();
        public static AnyLotQuantity     LotQuantity()     => new AnyLotQuantity();
        public static AnyProvenanceLink  ProvenanceLink()  => new AnyProvenanceLink();
        public static AnyTraceEntry      TraceEntry()      => new AnyTraceEntry();
        public static AnyTransformation  Transformation()  => new AnyTransformation();
        public static AnyBatchGenealogy  BatchGenealogy()  => new AnyBatchGenealogy();

    }

}
