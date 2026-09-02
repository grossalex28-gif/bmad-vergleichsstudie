using A1Testsuite.Basisklassen;

namespace A1Testsuite.Implementierungen;

public sealed class b_bmad_1_Tests : ProjektBTestsBase
{
    public b_bmad_1_Tests() : base(new BBmad1Adapter("http://localhost:5001")) { }
}
