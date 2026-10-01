---
name: levantar-issue
description: Fase 1 do fluxo do ProjetoVN. Levanta tudo o que a sessão de execução precisa para resolver uma issue do GitHub e registra em um comentário na issue. Use quando o Matheus disser "/levantar-issue N", "levantar a issue N" ou pedir o levantamento de uma issue.
argument-hint: <número da issue>
disable-model-invocation: true
---

# Levantar a issue #$ARGUMENTS

Você é a sessão de **levantamento**. O seu entregável é um comentário na issue que permita a uma sessão de execução, com contexto zerado, resolver a issue sem precisar descobrir nada. **Você não altera código.**

Se `$ARGUMENTS` estiver vazio, pergunte o número da issue e pare.

## 1. Conferir o estado

```bash
gh issue view $ARGUMENTS --comments --json number,title,body,labels,milestone,comments,state
```

- A issue precisa estar aberta e com a label `estado:levantamento`.
- Se estiver em outro estado, **não comece**. Diga em que estado ela está e qual é o comando certo (`estado:pronta` → `/executar-issue`, `estado:em-revisao` → `/revisar-issue`). Só siga se o Matheus mandar explicitamente, e registre isso no comentário final.
- Confira as dependências listadas em "Depende de": cada uma precisa estar fechada. Se alguma estiver aberta, avise o Matheus e pare.
- Confira se há outra issue com a label `estado:em-execucao` ou `estado:em-revisao`. Se houver, lembre a regra de uma issue por vez e pergunte se ele quer seguir.

## 2. Ler o contexto

Leia, nesta ordem:

1. `CLAUDE.md`
2. `docs/agentes/fluxo-de-trabalho.md`
3. `docs/arquitetura/decisoes.md`
4. `docs/arquitetura/visao-geral.md`
5. A seção da mecânica em `docs/jogo/mecanicas.md`
6. Os documentos citados na issue
7. O README de cada módulo envolvido
8. O código envolvido, inteiro, e os testes que o cobrem

Se a issue tocar no Editor, em cenas ou em prefabs, carregue a skill `unity:unity-cli` e leia `docs/agentes/unity-cli.md`. Inspecione as cenas e os prefabs reais pelo CLI; não suponha a estrutura deles.

## 3. Levantar

Fatos são trabalho seu: leia, procure, execute. Só vira pergunta para o Matheus o que é **decisão dele**.

Responda a cada ponto com evidência (caminho de arquivo, trecho, resultado de comando):

- O que existe hoje que a issue altera? Onde?
- Quais decisões `D-xx` limitam a solução?
- Qual é a solução mais simples que cumpre todos os critérios de aceite? Qual alternativa você descartou e por quê?
- O que é C# puro (e portanto pede teste antes do código) e o que é MonoBehaviour, cena ou prefab (verificado em Play Mode)?
- Há ligação de `UnityEvent`, renomeação, mudança de asmdef ou de campo serializado? Então quais armadilhas de `docs/agentes/unity-cli.md` se aplicam?
- A issue exige algo que pede autorização (pacote, `ProjectSettings/`, asmdef, apagar conteúdo)?
- Os critérios de aceite estão completos e verificáveis? Se não, proponha a correção.
- A issue cabe em um PR revisável? Se não, proponha a divisão.

Para uma issue com a label `tipo:prova-de-conceito`, levante também as fontes externas (documentação oficial, com link) que a execução vai precisar.

## 4. Publicar o comentário

Escreva o comentário em um arquivo temporário e publique:

```bash
gh issue comment $ARGUMENTS --body-file <arquivo>
```

Use exatamente esta estrutura:

```markdown
## Levantamento

**Resumo:** <duas ou três frases: o que será feito e o que não será>

### Arquivos envolvidos
| Arquivo | Ação | O que muda |
|---|---|---|

### Decisões aplicáveis
- **D-xx:** <como ela limita esta issue>

### Abordagem
<a solução, em passos numerados>

**Alternativa descartada:** <qual, e por quê>

### Plano de testes
<para C# puro: a lista dos testes a escrever primeiro, cada um com o que prova>

### Roteiro de verificação em Play Mode
<passos concretos, cada um com o comando ou a chamada e o resultado esperado>

### Skills a usar
- <skill>: <para quê>

### Documentação a atualizar
- <arquivo>: <o que muda>

### Armadilhas
<as de docs/agentes/unity-cli.md que se aplicam, e qualquer outra que você tenha encontrado>

### Perguntas em aberto
<só decisões do Matheus, numeradas, cada uma com a sua recomendação; ou "Nenhuma.">
```

## 5. Passar o bastão

Sem perguntas em aberto:

```bash
gh issue edit $ARGUMENTS --remove-label "estado:levantamento" --add-label "estado:pronta"
```

Com perguntas em aberto:

```bash
gh issue edit $ARGUMENTS --remove-label "estado:levantamento" --add-label "estado:aguardando-resposta"
```

## 6. Corrigir a documentação, se preciso

Se você encontrou documentação errada ou desatualizada, corrija em uma branch `docs/issue-$ARGUMENTS-levantamento`, abra um PR só de documentação e cite-o no comentário. Não misture com código.

## 7. Encerrar

Diga ao Matheus, em poucas linhas: o que o levantamento concluiu, se há perguntas para ele e qual é o próximo comando (`/executar-issue $ARGUMENTS`, em uma conversa nova).
