using A1Testsuite.Basisklassen;

namespace A1Testsuite.Implementierungen;

public sealed class a_solo_3_Tests : ProjektATestsBase
{
    public a_solo_3_Tests() : base(new ASolo3Adapter("http://localhost:5001")) { }
}
