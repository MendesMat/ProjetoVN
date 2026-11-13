# NOTURNA — UI nova: escolhas, menu principal, botão e caixa de histórico

Entrega do artista para o pedido: **botão de escolha, menu principal, botão de log de conversa e caixa de log de conversa**.

Tudo foi composto em **3840×2160** (Figma, página `03 · Composições 4K`) por cima da cena de referência que o projeto já usa: personagens alinhados pela base, caixa de diálogo na mesma posição. Cada sprite foi exportado **no tamanho em que ficou na composição**.

## Onde cada parte da entrega mora

A entrega original (uma pasta com os arquivos abaixo) chegou fora do repositório. Cada parte tem um destino:

| Parte da entrega | Onde fica |
|---|---|
| Este guia | `docs/arte/interface/guia-da-interface.md` |
| `CAMADA_DE_HORROR.md` | `docs/arte/interface/camada-de-horror.md` |
| As 3 telas montadas (menu, diálogo com escolhas, histórico aberto) | `docs/arte/interface/referencia_cena01_menu_4k.png`, `referencia_cena02_dialogo_escolhas_4k.png`, `referencia_cena03_historico_4k.png` |
| As 3 referências de horror (diálogo no nível 2, menu no nível 4, componentes por nível) | `docs/arte/interface/horror_*.png` |
| Fontes Atkinson Hyperlegible Next e Cinzel, com as licenças | `Assets/Fonts/AtkinsonHyperlegibleNext/` e `Assets/Fonts/Cinzel/` (#113) |
| 20 sprites da UI normal (`sprites_4k/`), sem texto | entram no repositório com a issue de cada tela: os quatro `escolha_*` já estão em `Assets/UI/` (#114), assim como os dez da #115 (`pilula_*`, `icone_folhinha`, `icone_fechar`, `painel_historico`, `rolagem_trilho`, `rolagem_alca` e `balao_branco_9slice`); os quatro da #116 (`fundo_menu_provisorio`, `logo_noturna_menu`, `menu_ornamento_lua` e `menu_sublinhado`) também; sobram em `Adicionar/` os de horror e os ainda sem tela |
| 9 sprites de horror, Special Elite e VT323 | entram com a issue da camada de horror (#117) |

Até lá, os sprites e as fontes de horror ficam só na cópia local do programador.

- **Figma** (arquivo `visual-mockup-luna1`): https://www.figma.com/design/njOlIvuDzQdfnjMtXPag9R/visual-mockup-luna1
  - `02 · UX Moodboard + Tipografia` — referências, princípios e os Text Styles `NOTURNA/…`
  - `03 · Composições 4K` — componentes com todos os estados, as 3 cenas e a prancha de exportação
  - `04 · Camada de horror` — componentes por nível de corrupção e as cenas de exemplo
- **Fontes** (Atkinson, Cinzel e VT323 são licença OFL, Special Elite é Apache 2.0 — todas livres para jogo comercial):
  [Atkinson Hyperlegible Next](https://fonts.google.com/specimen/Atkinson+Hyperlegible+Next) ·
  [Cinzel](https://fonts.google.com/specimen/Cinzel) ·
  [Special Elite](https://fonts.google.com/specimen/Special+Elite) ·
  [VT323](https://fonts.google.com/specimen/VT323)

**Camada de horror:** [camada-de-horror.md](camada-de-horror.md).

**Ordem de implementação no projeto:** fontes e caixa de diálogo (#113) → escolhas (#114) → botão e painel do histórico (#115) → menu principal (#116) → camada de horror (#117, quando o roteiro pedir). As issues citadas no restante do guia como "#11/#65", "#25/#55" e "#17" são as que criaram a interface atual.

**Conversão para o Canvas de 1920×1080:** todo valor de 4K ÷ 2. Para os sprites, use **Pixels Per Unit Multiplier = 2** na `Image` (ou importe com o dobro de PPU). As bordas de 9-slice abaixo estão em pixels do sprite (4K).

---

## 0. Checklist de importação no Unity

**Fontes → TextMeshPro**, um font asset por arquivo `.ttf`:

| Arquivo | Font Asset sugerido | Uso |
|---|---|---|
| `AtkinsonHyperlegibleNext-Regular.ttf` | `Atkinson SDF` (principal) | fala, balões, itens |
| `AtkinsonHyperlegibleNext-Medium.ttf` | `Atkinson Medium SDF` | texto das escolhas |
| `AtkinsonHyperlegibleNext-Bold.ttf` | `Atkinson Bold SDF` | placa de nome, botões pílula, nome no balão |
| `AtkinsonHyperlegibleNext-Italic.ttf` | `Atkinson Italic SDF` | narração e pensamento |
| `AtkinsonHyperlegibleNext-MediumItalic.ttf` | `Atkinson Medium Italic SDF` | opção de ação `#acao` |
| `Cinzel-Bold.ttf` | `Cinzel Bold SDF` | menu principal e títulos (só tem maiúsculas/versalete) |
| `SpecialElite-Regular.ttf` | `SpecialElite SDF` | horror — voz errada |
| `VT323-Regular.ttf` | `VT323 SDF` | horror — sistema |

- **Atlas dinâmico, sem conjunto de caracteres (correção da #113).** O conjunto "Extended ASCII" do Font Asset Creator é Latin-1 (U+0020 a U+00FF): cobre os acentos do português, mas **não** cobre `“ ” ‘ ’ … — – • €`. As fontes têm esses caracteres (conferido no `cmap` dos seis `.ttf` usados na interface); o que não os tem é o atlas gerado com esse conjunto, e o texto cairia na fonte de reserva do TextMeshPro. Por isso o projeto cria cada font asset pelo menu `Assets ▸ Create ▸ TextMeshPro ▸ Font Asset ▸ SDF`, que o deixa **Dynamic**: o glifo é gerado do `.ttf` na primeira vez que aparece. Configuração: Render Mode SDFAA, padding 9, atlas 1024×1024, **Multi Atlas Textures** ligado, **Clear Dynamic Data On Build** ligado (o padrão do `TMP Settings` do projeto) e **Include Font Data** ligado no `.ttf`.
- No `Atkinson SDF`, preencha a tabela **Font Weights**: Regular → Italic = `Atkinson Italic SDF`; Medium (500) → Regular/Italic = `Atkinson Medium SDF` / `Atkinson Medium Italic SDF`; Bold (700) → `Atkinson Bold SDF`. Assim o `FontStyles.Italic` que o código já usa pega o itálico de verdade, sem inclinação falsa.
- Para a camada de horror, deixe `SpecialElite SDF` e `VT323 SDF` em `Resources/Fonts & Materials` para o roteiro poder usar `<font="SpecialElite SDF">…</font>`.

**Sprites** (todos os PNG de `sprites_4k/` e `horror/sprites_4k/`):

- Texture Type **Sprite (2D and UI)**, Sprite Mode Single, Mesh Type **Full Rect** (obrigatório para 9-slice), Filter Bilinear, Compression None (ou High Quality), Max Size 4096 no `fundo_menu`, na caixa e no `painel_historico.png` (2640×1854; com 2048 ele seria reduzido e perderia nitidez — correção da #113); o resto 2048.
- Bordas de 9-slice no Sprite Editor conforme as tabelas abaixo (em pixels do PNG).
- Na `Image`: Image Type **Sliced** e **Pixels Per Unit Multiplier = 2** (os PNG são 4K, o Canvas é 1920×1080).

**Onde cada sprite entra no que já existe:**

| Hoje no projeto | Troca por |
|---|---|
| `HistoryButton` usando o sprite `Nameplate` (arte provisória) | `pilula_*` + `Image` filha com `icone_folhinha` |
| `CloseButton` do histórico (mesmo sprite provisório) | `pilula_*` + `icone_fechar`, texto “Fechar” |
| `Assets/UI/ChatBubble.svg` no `BubbleTemplate` | `balao_branco_9slice.png` (borda 48 = os mesmos 24 em 1080p) |
| `DialogueHistory` fundo preto 85% | continua; por cima entra o `painel_historico.png` |
| `Scrollbar` (cores chapadas) | `rolagem_trilho` e `rolagem_alca` (mesmas cores, agora com cantos) |
| `choiceButtonObjects[0..3]` | `escolha_*` via Sprite Swap |
| botões do `Menu.unity` (`MenuButtonUI`) | texto Cinzel + `decorateImage` = objeto `Highlight` com `menu_ornamento_lua` + `menu_sublinhado` (#116) |
| `Title Wallpaper B` e o logo de outro jogo no `Title Window` | `fundo_menu_provisorio` no `Canvas`, atrás das janelas, e `logo_noturna_menu` (#116) |

## 1. Fontes (todas gratuitas para uso comercial)

| Uso | Fonte | 4K | 1080p |
|---|---|---|---|
| Itens do menu principal (caixa alta, espaçamento 14%) | Cinzel Bold | 92 | 46 |
| Título de tela (“HISTÓRICO”, espaçamento 14%) | Cinzel Bold | 72 | 36 |
| Placa de nome | Atkinson Hyperlegible Next Bold | 68 | 34 |
| Fala na caixa | Atkinson Hyperlegible Next Regular | 92 | 46 |
| Narração e pensamento (itálico) | Atkinson Hyperlegible Next Italic | 92 | 46 |
| Texto do botão de escolha | Atkinson Hyperlegible Next Medium | 72 | 36 |
| Opção de ação `#acao` | Atkinson Hyperlegible Next **Medium Italic** | 72 | 36 |
| Botões pílula (Histórico, Fechar) | Atkinson Hyperlegible Next Bold | 56 | 28 |
| Texto do balão do histórico | Atkinson Hyperlegible Next Regular | 68 | 34 |
| Nome dentro do balão | Atkinson Hyperlegible Next Bold | 60 | 30 |
| Item recebido/entregue no histórico | Atkinson Hyperlegible Next Regular | 56 | 28 |
| **Horror** — fala corrompida | Special Elite Regular | 92 | 46 |
| **Horror** — nome corrompido / aviso de sistema | VT323 Regular | 104 | 52 |

Entrelinha 135% em todas. **No TextMeshPro o campo `Line Spacing` soma ao espaçamento natural da fonte, em % do tamanho** (0 = a altura de linha da própria fonte), então os 135% do desenho viram: **5** na Atkinson (altura de linha 1,30 em: `hhea` 984/−316 em 1000, logo 1,30 + 0,05 = 1,35) e **0** na Cinzel (1,348 em). A Atkinson tem **itálico de verdade**: o Italic e o Medium Italic são Font Assets próprios, ligados na tabela `Font Weights` do `Atkinson SDF`, para o `FontStyles.Italic` usar a fonte certa em vez de inclinar na marra. Os textos usam o `Atkinson SDF` e chegam às outras faces pelo estilo (`FontStyles.Italic`, `Bold`, `<b>`). Os acentos do português e as aspas, o travessão e as reticências saem do atlas dinâmico (ver a seção 0).

## 2. Cores

| Token | Hex | Onde |
|---|---|---|
| Plum | `#2A1025` | base da pílula, texto sobre rosa |
| Painel | `#24111F` (82–90%) + véu `#D89ABD` (30–42%) | vidro da caixa, escolha, painel do histórico |
| Rosa borda | `#DE7EB8` | contorno do vidro, sublinhado do menu |
| Rosa hover | `#D894C4` | estado hover/selecionado |
| Branco rosado | `#FFF3FB` | texto, contorno da pílula |
| Texto da fala | `#F7EBF5` | DialogueText |
| Sombra cel | `#10060F` a 72% | sombra dura deslocada (sem desfoque), já está nos sprites |
| Âmbar | `#D9A441` | só a lua do menu |
| Horror | `#FF4F7A` | só nas variantes corrompidas |

---

## 3. Botão de escolha (`DialogueUIController.choiceButtonObjects`, 4 fixos)

**Sprites** (1560×164 em 4K = 780×82 em 1080p; corpo 150 + 14 de sombra):

| Estado do `Button` (Transition = Sprite Swap) | Sprite | Cor do texto |
|---|---|---|
| Normal (Target Graphic) | `escolha_normal.png` | `#FFF3FB` |
| Highlighted **e** Selected | `escolha_hover.png` (já tem lua à esquerda e estrela à direita) | `#2A1025` |
| Pressed | `escolha_pressionado.png` | `#2A1025` |
| Disabled (opção com `<<if>>` falso) | `escolha_desabilitado.png` (borda tracejada) | `#FFF3FB` a 42% |

- **9-slice:** esquerda 170, direita 170, cima 0, baixo 0 (altura fixa; as bordas laterais protegem a lua e a estrela).
- **Texto:** centralizado, margem lateral 150 (75 em 1080p) para não encostar na lua/estrela.
- **Cor do texto por estado:** o `Button` sozinho só troca o sprite. Para o texto ir para plum no hover, dá para reaproveitar o padrão do `UISelectableBase`/`MenuButtonUI` (que já separa hover e seleção) nos quatro botões.
- **Posição na composição:** coluna centralizada, topo em y 480 (240 em 1080p), x 1140 (570), espaçamento 30 (15). Quatro botões ocupam y 480–1226, logo acima da caixa (topo do painel em 1344).
- Na cena, o `>>` some quando as opções abrem (como o código já faz) e o protagonista fica em destaque.

## 4. Botão Histórico (`openHistoryButton`) e botão Fechar

Mesmo sprite para os dois (pílula 460×164 em 4K = 230×82 em 1080p; corpo 150 + 14 de sombra):

| Estado | Sprite | Cor do texto/ícone |
|---|---|---|
| Normal | `pilula_normal.png` | `#FFF3FB` |
| Highlighted/Selected | `pilula_hover.png` | `#2A1025` |
| Pressed | `pilula_pressionado.png` | `#FFF3FB` |
| Disabled (fala sendo revelada) | `pilula_desabilitado.png` (já vem a 42%) | `#FFF3FB` |

- **9-slice:** 75 em todos os lados.
- **Ícone** é uma `Image` filha, não está no sprite: `icone_folhinha.png` (Histórico) e `icone_fechar.png` (Fechar), 72×72 (36 em 1080p), x 62 (31) a partir da esquerda, centrado na vertical. Os ícones são brancos, pinte pelo `Image.color` junto com o texto.
- **Texto** alinhado à esquerda a partir de x 156 (78).
- **Histórico na composição:** canto superior esquerdo em (3016, 1281) — espelha a placa de nome, montado na borda de cima da caixa, 84 px (42) antes da borda direita do painel.
- **Fechar na composição:** canto superior esquerdo em (1690, 95), centralizado, atravessando a borda de cima do painel do histórico como uma aba.

## 5. Caixa de histórico (`DialogueHistoryView`)

| Peça | Arquivo | Tamanho 4K (1080p) | 9-slice | Posição 4K (1080p) |
|---|---|---|---|---|
| Escurecer a tela | — (Image preta 85%, como já está) | tela cheia | — | — |
| Painel de vidro | `painel_historico.png` | 2640×1854 (1320×927) | cima 160, resto 80 | (600, 165) → (300, 82) |
| Título “HISTÓRICO” | TMP, Cinzel Bold 72 (36), 14% | — | — | x 190, y 60 dentro do painel (95, 30) |
| Área de rolagem (`Scroll`) | — | 2440×1530 (1220×765) | — | (680, 390) → (340, 195) |
| Trilho da barra | `rolagem_trilho.png` | 16×1500 (8×750) | 8 cima/baixo | x 3150 (1575) |
| Alça da barra | `rolagem_alca.png` | 16×420 (8×210) | 8 cima/baixo | — |
| Balão | `balao_branco_9slice.png` (branco, para o `Image.color` pintar) | 200×200 | **48** em todos (= 24 em 1080p, igual ao antigo `ChatBubble.svg`, removido na #115) | — |

Balões, como o código já faz: personagem à esquerda na `Name Color`, protagonista à direita, narração e item no centro no `neutralBubbleColor` com contorno `#FFF0FB` 18%. **No projeto (#115)** o neutro é `#2E1827` **opaco** (não `#24111F` 82%, que a referência não mostra e que deixaria o contorno aparecer através do balão), o pensamento é `#3E2641` opaco, e os dois levam o contorno. Padding do balão 56/56/32/32 em 4K = 28/28/16/16 (igual ao atual). Espaçamento entre balões 32 (16).

O painel ficou um pouco maior que o `Scroll` atual para caber o título e a aba Fechar. **Correção da #115:** não encolha o painel para 1240×860. O 9-slice só preserva a arte no tamanho nativo (1320×927 em 1080p): a estrela do título (x 112–172 do PNG) atravessa a borda esquerda de 80, e os três pontinhos do canto de baixo atravessam a borda de baixo, então os dois se deformam fora do tamanho nativo.

**A estrela do título já está desenhada no `painel_historico.png`**, junto com a faixa clara do título e os pontinhos: o `Title` do projeto é só o texto "HISTÓRICO", e o `icone_estrela.png` não é usado nesta tela.

**Outra correção da #115:** a altura preferida de uma `Image` fatiada é a soma das bordas **sem** o multiplicador (96 com a borda 48 do balão), e isso vira piso de altura do balão no layout (balões de uma linha ficaram ~18 px mais altos que a referência). No projeto a `Image` do balão fica num filho `Background` que ignora o layout.

## 6. Menu principal (`Menu.unity`, `MenuButtonUI`)

| Peça | Arquivo / valor | Posição 4K (1080p) |
|---|---|---|
| Fundo provisório | `fundo_menu_provisorio.png` 3840×2160 | tela cheia |
| Logo | `logo_noturna_menu.png` 1760×487 (880×244) | (240, 250) → (120, 125) |
| Coluna de botões | Novo Jogo · Continuar · Opções · Sair | **x 300 (150) é a borda da lua**; o texto começa em 396 (198). Topo das maiúsculas em y 1004, 1188, 1372 e 1556 (502, 594, 686, 778): **passo de 92** por item (botão de 74 + espaçamento 18) |
| Versão | Atkinson Regular 40 (20), `#FFF3FB` 45% | (300, 2010) → (150, 1005) |

**Cada item (`MenuButtonUI`):**

- Texto Cinzel Bold 92 (46), caixa alta, espaçamento 14%.
- `normalTextColor` `#FFF3FB` a 92% · `highlightedTextColor` `#FFF3FB` · `disabledTextColor` `#FFF3FB` a 32% (Continuar sem save).
- `decorateImage` = `menu_ornamento_lua.png` (64×64 → 32), à esquerda do texto com 32 (16) de espaço, visível só no hover/seleção.
- Hover também liga `menu_sublinhado.png` (638×64 → 319×32) logo abaixo do texto.
- Sombra cel no hover: Underlay do material TMP em `#DE7EB8`, deslocamento 0/−8 em 4K (−4 em 1080p), **softness 0** (sombra dura, sem desfoque). **No projeto (#116)** é o *material preset* `Assets/Fonts/Cinzel/Cinzel Bold SDF - Menu Destaque.mat`, com `_UnderlayOffsetY` −0,87, Dilate 0, Softness 0 e **`_WeightBold = 0`**: com o 0,75 do material padrão do `Cinzel Bold SDF` o teto do underlay é 3,7 px em 46 pt, e os 4 px só saem com `_WeightBold = 0` (ver `unity-cli.md`). O `MenuButtonUI` o troca no destaque pelo campo `highlightedLabelMaterial`.
- O sublinhado (319×32) tem o topo 44 abaixo do topo das maiúsculas (a linha rosa fica ~58 abaixo) e fica pendurado no texto; a lua é centrada na altura das maiúsculas.
- Não precisa de sprite de fundo no botão (o `Image` pode ser transparente e servir só de área de clique).

## 7. Ícones extras

`icone_lua.png` e `icone_estrela.png` (64×64, brancos) são os ornamentos da linguagem nova (lua = hover/escolha, estrela = seleção/título). Podem ser reaproveitados em outras telas.

## 8. Camada de horror (resumo — detalhes em `horror/CAMADA_DE_HORROR.md`)

Um único inteiro **`nivel_corrupcao` (0–4)** no `StoryState`, mudado por comando do roteiro (ex.: `<<corrupcao 2>>`). Cada tela **lê** o valor e troca sprites/fontes — a UI continua só lendo estado.

| Sprite | Tamanho 4K | Substitui | Nível |
|---|---|---|---|
| `horror_escolha_n3.png` | 1560×190 (corpo 150 + sombra; inclui linha de glitch) | `escolha_normal` | 3 |
| `horror_escolha_n4.png` | 1560×190 | `escolha_desabilitado` | 4 |
| `horror_placa_vermelha.png` | 511×150, 9-slice 75 | placa de nome | 3–4 |
| `horror_caixa_dialogo_n3.png` | 3385×879 (mesma posição da caixa atual, 190/1281 em 4K; linhas de glitch já embutidas) | caixa de diálogo | 3–4 |
| `horror_aviso_vermelho_9slice.png` | 400×120, 9-slice 54 | aviso curto | 4 |
| `horror_sublinhado_vermelho.png` | 638×64 | `menu_sublinhado` | 3–4 |
| `horror_estrela_vermelha.png` | 64×64 | `icone_estrela` | 3–4 |
| `horror_varredura.png` | 64×4 (esticar na largura) | linha de glitch avulsa, por cima de qualquer peça | 1+ |
| `horror_fundo_menu_n4.png` | 3840×2160 | `fundo_menu_provisorio` | 4 |

Fontes de horror: **Special Elite** (fala fantasma / voz errada) e **VT323** (nome corrompido, avisos de sistema). Cor `#FF4F7A`.
Já dá sem código novo: opção desabilitada com texto de horror (ex.: “fugir”) e fala com fonte trocada via rich text do TMP (`<font="SpecialElite SDF">…</font>`).
**Cuidado:** nada de piscar rápido nem tela inteira vermelha — prever aviso de fotossensibilidade e opção “reduzir efeitos”.

## 9. Ainda provisório (vai ser trocado pela arte final)

- `fundo_menu_provisorio.png` e `horror_fundo_menu_n4.png` — fundos gráficos até sair a ilustração do menu.
- Logo do menu no nível 4 — hoje é o logo normal dessaturado; a versão corrompida (lua vazia, letras rachadas) está sendo desenhada pelo artista.
- Cenário do corredor nas referências — é o mockup antigo, só para dar contexto.
- Nada disso muda posições nem tamanhos: quando a arte final chegar é só trocar o PNG.

---

Qualquer valor aqui pode ser conferido no Figma: os componentes estão no frame **Componentes** (com todos os estados) e os sprites no frame **Exportação** da página `03 · Composições 4K`.
