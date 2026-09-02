using A1Testsuite.Basisklassen;

namespace A1Testsuite.Implementierungen;

public sealed class b_solo_2_Tests : ProjektBTestsBase
{
    public b_solo_2_Tests() : base(new BSolo2Adapter("http://localhost:5001")) { }
}
