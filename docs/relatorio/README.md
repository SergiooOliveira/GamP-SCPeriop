# Relatório / tese — GamP-SCPeriop

Documento LaTeX com o modelo da UPCA (`upca/lesi.sty`), o mesmo do relatório do Leva.

## Compilar

Requer MiKTeX (ou TeX Live) com `latexmk`. Na pasta `docs/relatorio`:

```bash
latexmk -pdf main.tex
```

O `latexmk` corre `pdflatex`, `bibtex` e `makeglossaries` as vezes necessárias (ver `.latexmkrc`).
No VS Code, a extensão **LaTeX Workshop** compila ao gravar.

> **MiKTeX:** não usar *Check for updates* no MiKTeX Console. As versões novas dos programas são
> bloqueadas pelo Smart App Control do Windows e partem a instalação.

## Estrutura

| Ficheiro | Conteúdo |
| --- | --- |
| `main.tex` | Preâmbulo, título, ordem dos capítulos |
| `resumo.tex` | Resumo e abstract, com palavras-chave |
| `chap1.tex` | Introdução: problema, solução, objetivos, âmbito |
| `chap2.tex` | Análise: pressupostos, processos BPMN, perfis, requisitos RF/RNF |
| `chap3.tex` | Utilizador principal e task model (HTA) |
| `chap4.tex` | Site map |
| `chap5.tex` | Sistema visual: logótipo, paleta, tipografia, grelha |
| `chap6.tex` | Arquitetura: monólito, rotas, autenticação, catálogo de endpoints, dados |
| `chap7.tex` | Conclusão e trabalho identificado |
| `acronimos.tex`, `glossario.tex` | Siglas e glossário (só aparecem os usados com `\gls`) |
| `biblio.bib` | Bibliografia |
| `cores.tex` | Paleta da aplicação (copiada de `wwwroot/css/app.css`) |
| `estilos-tikz.tex` | Estilos dos diagramas e a macro `\bpmnfigura` |
| `figuras/` | Diagramas em TikZ |
| `bpmn/` | Diagramas BPMN editáveis (`.bpmn`) |

## Editar e substituir os BPMN

Os dois processos existem em duas versões:

- `bpmn/percurso.bpmn` e `bpmn/avaliacao.bpmn` — BPMN 2.0, editáveis no
  [Camunda Modeler](https://camunda.com/download/modeler/) ou em <https://demo.bpmn.io>;
- `figuras/bpmn-percurso.tex` e `figuras/bpmn-avaliacao.tex` — o mesmo desenho em TikZ.

Para usar a versão do modelador no relatório, exporta o diagrama com o mesmo nome para a pasta `bpmn/`:

1. Abre `bpmn/percurso.bpmn` no modelador e edita.
2. Exporta como **PDF** (preferível) ou **PNG** para `bpmn/percurso.pdf` (ou `bpmn/percurso.png`).
3. Recompila. A macro `\bpmnfigura{percurso}` usa a imagem exportada se existir; se não, usa o TikZ.

O mesmo para `avaliacao`. Para voltar ao TikZ, apaga ou muda o nome do ficheiro exportado.
O Camunda Modeler exporta SVG e PNG; para PDF, abre o SVG no navegador e imprime para PDF,
ou converte com Inkscape (`inkscape percurso.svg -o percurso.pdf`).

## Por completar

Procurar `\porcompletar` no código (aparece a vermelho no PDF):

- [ ] Resumo e abstract (`resumo.tex`)
- [ ] Unidade curricular, orientador e restantes autores (`chap1.tex`, `main.tex`)
- [ ] Logótipo e regras de utilização (`chap5.tex`)
- [ ] Diagrama entidade-relação, se for pedido (`chap6.tex`)
- [ ] Confirmar o perfil do supervisor com utilizadores reais (`chap3.tex`)

## Origem dos dados

Os números do documento foram tirados do código em outubro de 2026: rotas (`@page`), endpoints
(atributos `[Http*]` dos controladores), cores (`app.css`), migrações (`Migrations/`). Se o código
mudar, rever sobretudo o catálogo de endpoints (`chap6.tex`) e a lista de ecrãs (`chap4.tex`).
