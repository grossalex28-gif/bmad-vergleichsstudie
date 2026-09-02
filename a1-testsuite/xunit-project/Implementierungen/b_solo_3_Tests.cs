using A1Testsuite.Basisklassen;

namespace A1Testsuite.Implementierungen;

public sealed class b_solo_3_Tests : ProjektBTestsBase
{
    public b_solo_3_Tests() : base(new BSolo3Adapter("http://localhost:5001")) { }
}
