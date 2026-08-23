# Incremento 6 — Portal administrativo

## Objetivo

Disponibilizar uma interface operacional segura para demonstrar os recursos do CommerceFlow sem expor o JWT ao JavaScript do navegador e sem acoplar o frontend diretamente aos microsserviços.

## Evolução

| Etapa | Entrega |
|---|---|
| 6.1 | Fundação Next.js, autenticação BFF, cookie HttpOnly e dashboard |
| 6.2 | Gestão de produtos, saldos e movimentações de estoque |
| 6.3 | Criação, consulta, acompanhamento e cancelamento de pedidos |
| 6.4 | Liveness, readiness, SLOs, ferramentas e diagnóstico operacional |
| 6.5 | Docker, Compose, CSP, CI, smoke test e Kubernetes |

## Decisões de segurança

- o navegador envia credenciais somente para o Route Handler de sessão;
- o token é armazenado em cookie `HttpOnly`, `SameSite=Lax` e `Secure` em HTTPS;
- chamadas de escrita rejeitam origens diferentes;
- o BFF adiciona o JWT somente nas chamadas servidor-servidor;
- nenhuma credencial real é incluída na imagem ou nos manifests;
- o portal publica CSP, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy` e remove `X-Powered-By`;
- o contêiner final executa como usuário não-root.

## Critérios de aceite

- `npm run lint` e `npm run build` terminam sem erros;
- `docker compose up --build -d` inicia o portal com os demais componentes;
- `/api/health/live` responde HTTP 200;
- uma chamada sem sessão ao BFF protegido responde HTTP 401;
- o login cria cookie HttpOnly e não devolve o JWT ao JavaScript;
- Estoque, Vendas e Observabilidade funcionam pelo BFF;
- o CI valida backend, frontend, Compose e as cinco imagens próprias;
- os manifests Kubernetes passam por `kubectl kustomize` e exigem TLS na borda;
- `scripts/test-increment6.ps1` conclui 10/10 verificações.

## Validação

```powershell
docker compose up --build -d
.\scripts\test-increment6.ps1
```

O resultado esperado é `10/10 - Teste funcional do Incremento 6 concluido com sucesso.` em verde.
