using A1Testsuite.Basisklassen;

namespace A1Testsuite.Implementierungen;

public sealed class a_bmad_3_Tests : ProjektATestsBase
{
    public a_bmad_3_Tests() : base(new ABmad3Adapter("http://localhost:5001")) { }
}
