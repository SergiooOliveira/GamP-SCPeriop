using System;
using System.Collections.Generic;
using System.Text;

namespace GamP_SCPeriop.Shared.Enum
{
    public enum ComponentStatus
    {
        Pending = 0,                // Cinza
        EmProgresso = 1,            // Azul
        Inconsistente = 2,          // Vermelho
        AbaixoDaMedia = 3,          // Laranja
        AcimaDaMedia = 4,           // Amarelo
        Consistente = 5             // Verde
    }
}
