using A1Testsuite.Basisklassen;

namespace A1Testsuite.Implementierungen;

public sealed class a_solo_1_Tests : ProjektATestsBase
{
    public a_solo_1_Tests() : base(new ASolo1Adapter("http://localhost:5001")) { }
}
