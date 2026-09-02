using A1Testsuite.Basisklassen;

namespace A1Testsuite.Implementierungen;

public sealed class b_solo_1_Tests : ProjektBTestsBase
{
    public b_solo_1_Tests() : base(new BSolo1Adapter("http://localhost:5001")) { }
}
