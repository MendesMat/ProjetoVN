---
name: executar-issue
description: Fase 2 do fluxo do ProjetoVN. Implementa uma issue do GitHub já levantada, em uma branch própria, guiada pela issue, pelos comentários e pela documentação, e abre o PR. Use quando o Matheus disser "/executar-issue N", "executar a issue N" ou pedir a implementação de uma issue.
argument-hint: <número da issue>
disable-model-invocation: true
---

# Executar a issue #$ARGUMENTS

Você é a sessão de **execução**. O seu entregável é um PR que cumpre os critérios de aceite da issue, e nada além deles. Você é guiado pela issue, pelos comentários dela e pela documentação do repositório.

Se `$ARGUMENTS` estiver vazio, pergunte o número da issue e pare.

## 1. Conferir o estado

```bash
gh issue view $ARGUMENTS --comments --json number,title,body,labels,milestone,comments,state
```

- A issue precisa estar aberta e com a label `estado:pronta` (primeira execução) ou `estado:mudancas-pedidas` (reexecução depois da revisão).
- Se estiver em outro estado, **não comece**. Diga em que estado ela está e qual é o comando certo. Só siga se o Matheus mandar explicitamente, e registre isso no PR.
- Precisa existir um comentário de **Levantamento**. Sem ele, pare e indique `/levantar-issue $ARGUMENTS`.
- Se o levantamento tem perguntas em aberto, as respostas do Matheus precisam estar nos comentários. Sem resposta, pare.
- Confira se há outra issue com a label `estado:em-execucao` ou `estado:em-revisao`. Se houver, lembre a regra de uma issue por vez e pergunte se ele quer seguir.

## 2. Ler o contexto

1. `CLAUDE.md`
2. `docs/agentes/fluxo-de-trabalho.md`
3. `docs/agentes/skills.md`
4. `docs/arquitetura/decisoes.md`
5. A issue **inteira** e **todos** os comentários, na ordem
6. Os arquivos e documentos listados no levantamento

Numa reexecução, leia também o parecer da revisão no PR:

```bash
gh pr list --state open --json number,headRefName,url --jq '.[] | select(.headRefName | startswith("issue-$ARGUMENTS-"))'
```

```bash
gh pr view <número do PR> --comments
```

## 3. Preparar

Primeira execução:

```bash
git checkout main
```

```bash
git pull --ff-only
```

```bash
git checkout -b issue-$ARGUMENTS-<resumo-curto>
```

```bash
gh issue edit $ARGUMENTS --remove-label "estado:pronta" --add-label "estado:em-execucao"
```

Reexecução: faça checkout da branch do PR existente e troque `estado:mudancas-pedidas` por `estado:em-execucao`.

Confirme a linha de base antes de mudar qualquer coisa:

```bash
unity status
```

```bash
unity command run_tests --mode EditMode --timeout 180
```

Se a linha de base já falha, pare e avise o Matheus.

## 4. Implementar

Carregue as skills antes de escrever:

- `anthropic-skills:clean-code`: sempre que houver C#.
- `anthropic-skills:test-driven-development`: para toda classe C# pura. Rode o ciclo em modo autônomo: teste que falha, código mínimo, refatoração.
- `unity:unity-cli`: antes de qualquer comando no Editor.
- As skills listadas no levantamento.

Regras:

- **Siga o levantamento.** Um desvio pequeno e justificado é aceito e vai descrito no PR. Um desvio grande é sinal de que a issue está errada: veja o passo 7.
- **Decisão do projeto vence skill.** Releia a seção "Como a `clean-code` se aplica aqui" de `docs/agentes/skills.md`.
- **Só o escopo da issue.** Um problema fora do escopo vira issue:

  ```bash
  gh issue create --title "<título>" --label "triagem" --body "<o que foi visto, onde, e em que issue apareceu>"
  ```

- **Editor aberto:** altere cenas, prefabs e assets pelo Unity CLI. As exceções e as armadilhas estão em `docs/agentes/unity-cli.md`.
- **Precisa de autorização** (pacote, `ProjectSettings/`, asmdef, apagar conteúdo) que a issue não deu: pergunte ao Matheus e espere.
- **Commits pequenos**, em português, no formato `#$ARGUMENTS: <o que mudou>`.

## 5. Verificar

Faça de verdade; não declare sem executar.

1. Recompile e confira que não há erro: `unity command recompile`, depois `unity command recompile_status`.
2. Rode a suíte inteira: `unity command run_tests --mode EditMode --timeout 180`.
3. Execute o roteiro de verificação em Play Mode do levantamento, passo a passo. Comece por `Application.runInBackground = true` se algum passo depender de frames.
4. Confira cada critério de aceite da issue, um por um.
5. Confira `git status`: todo arquivo novo em `Assets/` tem o seu `.meta`, e nenhum arquivo alterado pelo Editor sem relação com a issue entra no commit.

Se algo não pôde ser verificado, diga exatamente o quê e por quê no PR. Não marque como feito.

## 6. Documentar e abrir o PR

Atualize a documentação listada no levantamento, no mesmo PR.

```bash
git push -u origin HEAD
```

Abra o PR preenchendo o modelo de `.github/PULL_REQUEST_TEMPLATE.md`, com `Closes #$ARGUMENTS` no corpo:

```bash
gh pr create --title "#$ARGUMENTS: <título da issue>" --body-file <arquivo>
```

```bash
gh issue edit $ARGUMENTS --remove-label "estado:em-execucao" --add-label "estado:em-revisao"
```

Numa reexecução, não abra outro PR: dê push na mesma branch e comente no PR o que foi corrigido, item por item do parecer.

## 7. Se a issue estiver errada

Quando o plano não funciona, um critério não faz sentido ou a solução exige mudar uma decisão `D-xx`:

1. **Pare de implementar.** Não improvise uma solução diferente.
2. Comente na issue: o que você tentou, o que encontrou (com evidência) e as opções que enxerga.
3. Devolva a issue:

   ```bash
   gh issue edit $ARGUMENTS --remove-label "estado:em-execucao" --add-label "estado:aguardando-resposta"
   ```

4. Deixe a branch como está, com o trabalho parcial commitado e enviado, sem abrir PR.

## 8. Encerrar

Diga ao Matheus, em poucas linhas: o que foi feito, o resultado dos testes e da verificação, o que ficou sem verificar, o link do PR e o próximo comando (`/revisar-issue $ARGUMENTS`, em uma conversa nova). **Você não faz o merge.**
