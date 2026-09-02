using A1Testsuite.Basisklassen;

namespace A1Testsuite.Implementierungen;

public sealed class a_solo_2_Tests : ProjektATestsBase
{
    public a_solo_2_Tests() : base(new ASolo2Adapter("http://localhost:5001")) { }
}
