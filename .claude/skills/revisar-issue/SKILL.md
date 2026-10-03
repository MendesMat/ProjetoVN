---
name: revisar-issue
description: Fase 3 do fluxo do ProjetoVN. Revisa o PR de uma issue do GitHub contra a issue, os comentários e a documentação, e publica um parecer no PR. Não altera código. Use quando o programador disser "/revisar-issue N", "revisar a issue N" ou pedir a revisão de uma issue ou do PR dela.
argument-hint: <número da issue>
disable-model-invocation: true
---

# Revisar a issue #$ARGUMENTS

Você é a sessão de **revisão**: um segundo par de olhos, independente de quem implementou. O seu entregável é um parecer no PR. **Você não altera código, não faz commit e não faz merge.** Um defeito trivial também é só relatado; é essa independência que justifica a sessão separada.

Se `$ARGUMENTS` estiver vazio, pergunte o número da issue e pare.

## 1. Conferir o estado

```bash
gh issue view $ARGUMENTS --json number,title,body,labels,milestone,comments,state
```

```bash
gh pr list --state open --json number,title,headRefName,url --jq '.[] | select(.headRefName | startswith("issue-$ARGUMENTS-"))'
```

- A issue precisa estar com a label `estado:em-revisao` e ter um PR aberto, de uma branch `issue-$ARGUMENTS-…`.
- Se estiver em outro estado, **não comece**. Diga em que estado ela está e qual é o comando certo.

## 2. Ler o contexto, antes do código

Forme a sua expectativa antes de ver a solução:

1. `CLAUDE.md`
2. `docs/arquitetura/decisoes.md`
3. `docs/agentes/skills.md` (a seção sobre a `clean-code` neste projeto)
4. A issue inteira e todos os comentários: critérios de aceite, levantamento, respostas do programador
5. Os documentos citados na issue e no levantamento

Só então leia o PR:

```bash
gh pr view <PR> --comments
```

```bash
gh pr diff <PR>
```

Leia também os arquivos alterados por inteiro, não só o diff: um diff correto pode estar no lugar errado.

## 3. Executar

Faça checkout da branch do PR e rode de verdade:

```bash
gh pr checkout <PR>
```

```bash
unity status
```

```bash
unity command recompile
```

```bash
unity command run_tests --mode EditMode --timeout 180
```

Depois, execute o **roteiro de verificação em Play Mode** do levantamento, passo a passo, seguindo `docs/agentes/unity-cli.md` (comece por `Application.runInBackground = true` se algum passo depender de frames). Guarde a saída de cada passo: ela é a evidência do parecer.

Você não confia no que o PR diz que foi verificado. Você verifica de novo.

## 4. Conferir

Responda a cada pergunta com evidência (arquivo e linha, ou saída de comando):

1. **Critérios de aceite:** cada um foi cumprido? Um por um.
2. **Levantamento:** o código segue a abordagem? Um desvio está explicado no PR e faz sentido?
3. **Decisões:** alguma `D-xx` foi violada? Atenção especial a: mensagem usada como comando ou consulta (D-05), ScriptableObject alterado em runtime (D-02), interface ou abstração sem motivo concreto (D-03), manager em cena, estado estático sem reset (D-29), thread, `Task.Run` ou reflexão (D-23), acesso a arquivo fora do `GameSaveManager` (D-20).
4. **Testes:** para C# puro, existe teste, e ele falharia sem o código novo? Os testes limpam o estado estático no `SetUp` e no `TearDown`?
5. **Unity:** todo arquivo novo em `Assets/` tem `.meta`? Há arquivo do Editor alterado sem relação com a issue? Ligações de `UnityEvent` sobreviveram a renomeações e mudanças de asmdef?
6. **Autoria:** todo campo novo exposto no Inspector tem tooltip e validação?
7. **Escopo:** há algo no diff que a issue não pediu?
8. **Documentação:** os arquivos listados no levantamento foram atualizados e descrevem o que o código faz agora?
9. **Código limpo:** carregue `anthropic-skills:clean-code` e use a lista de revisão dela, lembrando que a convenção do projeto vence a skill.

Para ampliar a busca por defeitos, você pode usar a skill `code-review` sobre o PR e incorporar os achados que confirmar.

## 5. Publicar o parecer

Escreva o parecer em um arquivo temporário e publique no PR:

```bash
gh pr comment <PR> --body-file <arquivo>
```

Use exatamente esta estrutura:

```markdown
## Revisão da issue #N

**Veredito:** aprovado | mudanças pedidas

### Critérios de aceite
| Critério | Situação | Evidência |
|---|---|---|

### Testes e verificação
- Suíte EditMode: <resultado colado>
- Roteiro de Play Mode: <cada passo, com o resultado>
- Não foi possível verificar: <o quê e por quê, ou "nada">

### Achados
<em ordem de gravidade; cada um com arquivo:linha, o que está errado, por que importa e a menor correção>

**Bloqueiam o merge:**
1. …

**Não bloqueiam:**
1. …

### Fora do escopo
<problemas vistos que não pertencem a esta issue; viram issues com a label `triagem`>
```

O veredito é **mudanças pedidas** se qualquer critério de aceite falhou, qualquer teste falhou, uma decisão `D-xx` foi violada ou a documentação não foi atualizada. Preferência de estilo sem base em `docs/` não bloqueia.

## 6. Passar o bastão

Aprovado:

```bash
gh issue edit $ARGUMENTS --remove-label "estado:em-revisao" --add-label "estado:aprovada"
```

Mudanças pedidas:

```bash
gh issue edit $ARGUMENTS --remove-label "estado:em-revisao" --add-label "estado:mudancas-pedidas"
```

Crie uma issue com a label `triagem` para cada item de "Fora do escopo".

Volte para `main` ao terminar:

```bash
git checkout main
```

## 7. Encerrar

Diga ao programador, em poucas linhas: o veredito, o que bloqueia (se algo), e o próximo passo: o merge é dele se aprovado, ou `/executar-issue $ARGUMENTS` em uma conversa nova se há mudanças pedidas.
