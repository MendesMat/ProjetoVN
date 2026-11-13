# NOTURNA — Camada de horror da UI

**Regra principal:** o horror da interface só funciona porque a interface normal é doce, limpa e confiável. Ele é **raro, gradual e sempre ligado à história**. Cada momento corrompe **uma coisa só**; nunca tudo de uma vez.

Figma: página `04 · Camada de horror` (componentes por nível + duas cenas de exemplo). As três imagens de referência desta camada estão ao lado deste arquivo, em `docs/arte/interface/` (`horror_componentes_por_nivel.png`, `horror_exemplo_n2_dialogo_4k.png`, `horror_exemplo_n4_menu_4k.png`). Os sprites (`horror_*.png`) ainda não estão no repositório; entram com a issue da camada de horror (#117). As fontes Special Elite e VT323 também.

## Ingredientes

| Ingrediente | Significa | Uso |
|---|---|---|
| **Special Elite** (máquina de escrever) | voz errada | fala que não devia existir, frase repetida, texto "de outra pessoa" |
| **VT323** (sistema) | a máquina falando | nome corrompido, erro, data errada, arquivo (ref. Lain) |
| **Vermelho `#FF4F7A`** | algo está errado | substitui o rosa; nunca convive com ele na mesma peça |
| Linha de varredura (2–4 px, vermelho 40–60%) | glitch de um quadro | atravessa uma peça por instantes |
| Texto fantasma (cópia deslocada em vermelho 50%) | duplicação / eco | atrás do texto principal, deslocado 6–10 px |

## Níveis de corrupção (0–4)

| Nível | O que muda | Sensação |
|---|---|---|
| 0 | nada — UI toda doce | isca romântica |
| 1 | um glitch de um quadro (linha vermelha), texto que reescreve, lua do hover some | "foi impressão minha?" |
| 2 | Special Elite aparece em falas fantasmas e em uma opção; placa de nome riscada | desconforto |
| 3 | vermelho substitui o rosa nas bordas, nomes duplicados, histórico alterado | a interface mente |
| 4 | VT323 nos nomes e menus, "???????", menu e logo corrompidos, opções sem escolha | a interface é o monstro |

Cada nível inclui os anteriores, mas **em cena só se ativa o que a história pede**.

## Exemplos por peça

**Caixa de diálogo**
- Depois do `>>`, aparece uma linha extra em Special Elite que ninguém disse ("você não devia ter vindo") — e o histórico não a registra.
- O typewriter volta 3 letras e reescreve outra palavra ("Você acredita em ~~destino~~ mim?").
- A borda pisca vermelho por 1 quadro quando o Corpo Seco é citado.

**Placa de nome**
- "Luna" → riscado → "???????" em VT323 quando a identidade dela fica em dúvida.
- Duas placas sobrepostas ("Luna Luna").
- O nome escolhido pelo jogador aparece escrito errado uma vez, sem comentário.

**Botões de escolha**
- Três opções normais e uma quarta em Special Elite, desabilitada: "fugir" (o código já mostra opção indisponível desabilitada).
- Todas as opções dizem a mesma coisa com palavras diferentes.
- No hover, a lua some e fica só a estrela.

**Histórico**
- Uma fala antiga da Luna muda ao reabrir (agora em Special Elite).
- Um balão do protagonista que ele nunca falou.
- Rodapé "Luna está digitando…" com a Luna fora da cena.
- A rolagem não chega ao começo: há conversa "antes" do início.

**Menu principal**
- A lua do céu muda de fase, o halo âmbar some, "CONTINUAR?" com interrogação.
- Nível 4: logo corrompido (lua vazia, letras rachadas, sombras soltas, paleta dessaturada).
- Versão `v0.0` ou data impossível (31/02/2003).

**Avisos, save, transições**
- "Jogo salvo" seguido de `luna.dat atualizado` em VT323.
- Data do save em 1998. "Você recebeu: [ARQUIVO CORROMPIDO]".
- Transição com 1 quadro de estática; cursor de lua vira uma mão de madeira por 1 segundo; pausa com uma opção extra que some no hover.

## Implementação (para o programador)

- **Sem código novo:** opção desabilitada com texto de horror, falas com fonte trocada pelo roteiro (rich text do TMP), cores por personagem.
- **Proposta:** um único inteiro `nivel_corrupcao` (0–4) no `StoryState`, alterado por comando do roteiro (ex.: `<<corrupcao 2>>`). Cada tela **lê** esse valor e troca a variante de sprites/fontes (respeita a regra "a UI lê estado, só modifica chamando o dono").
- **Exigem lógica própria (depois):** histórico alterado, falas fantasmas fora do registro, cursor que muda, opção que some no hover.

## Cuidados

- **Nada de piscar rápido nem tela inteira vermelha estourando** (risco de fotossensibilidade). Aviso no início do jogo e opção "reduzir efeitos".
- Texto sempre legível; Special Elite só em frases curtas.
- O glitch nunca esconde informação necessária: o que a "fantasma" diz é extra, nunca a pista principal.
