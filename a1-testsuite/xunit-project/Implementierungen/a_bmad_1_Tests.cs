using A1Testsuite.Basisklassen;

namespace A1Testsuite.Implementierungen;

public sealed class a_bmad_1_Tests : ProjektATestsBase
{
    public a_bmad_1_Tests() : base(new ABmad1Adapter("http://localhost:5001")) { }
}
