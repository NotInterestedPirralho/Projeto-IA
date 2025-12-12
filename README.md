# 🤖 Projeto - IA: Documentação da Inteligência Artificial em ClashBound

Esta documentação aborda os conceitos de Inteligência Artificial (IA) e Pathfinding implementados no sistema de inimigos e nos power-ups do projeto, em concordância com os tópicos do Projeto Aplicado.

## 1. Máquina de Estados Finitos (FSM)

A Máquina de Estados Finitos é o padrão de arquitetura comportamental mais comum e fundamental na vossa IA. Ela permite que os inimigos alternem entre um conjunto limitado de estados pré-definidos, garantindo que o comportamento seja previsível e controlado.

### 1.1 Implementação no Código

O FSM é implementado nos scripts EnemyAI.cs e EnemyAI_BFS.cs da seguinte forma:

* **Definição de Estados:** É usado um `enum` público para definir os estados possíveis do inimigo:
    ```csharp
    public enum AIState
    {
        Idle,       // O inimigo está parado
        Patrol,     // O inimigo está a patrulhar uma área
        Chase,      // O inimigo está a perseguir um jogador
        Attack,     // O inimigo está a executar o ataque
        Stunned     // O inimigo foi atordoado e está inativo
    }
    ```
* **Controlo de Estado:** O método Update() utiliza uma estrutura `switch (currentState)` para executar a função de handling correspondente a cada estado em cada frame.
    ```csharp
    switch (currentState)
    {
        case AIState.Patrol:
            HandlePatrol();
            break;
        // ... (outros estados)
    }
    ```
* **Transição de Estados:** As transições ocorrem através de condições lógicas dentro das funções de handling.
    * Exemplo (Patrol $\rightarrow$ Chase): Se a função CanSeePlayer() retornar verdadeiro, o estado muda.
    * Exemplo (Stunned $\rightarrow$ Chase): Esta transição é gerida por uma Corrotina (ResetStun) para reverter o estado após um período de tempo definido.

### 1.2 Função no Contexto Multiplayer

A FSM é executada exclusivamente pelo Master Client (Host). Isto garante que a lógica de IA (decisão de movimento e ataque) é centralizada e autoritária, mantendo a sincronização entre todos os clientes.

## 2. Pathfinding: Busca em Largura (BFS)

O algoritmo *Breadth-First Search* (Busca em Largura) é usado no script EnemyAI_BFS.cs para permitir que o inimigo navegue em ambientes de plataforma complexos (contornando obstáculos e chegando a diferentes níveis de plataformas), calculando o caminho mais curto em número de nós.

### 2.1 Implementação do Algoritmo

A implementação do BFS envolve três etapas principais:

#### A. O Algoritmo de Busca (`RunBFS`)

O método `RunBFS` é o núcleo do Pathfinding:

* **Grelha Virtual:** As posições do mundo (`Vector2`) são convertidas em coordenadas de grelha (`Vector2Int`) usando a `cellSize`.
* **Estruturas de Dados:**
    * `Queue<Vector2Int> frontier`: A fila FIFO (First-In, First-Out) é usada para explorar os nós vizinhos sequencialmente, garantindo que o BFS encontre o caminho com o menor número de passos.
    * `Dictionary<Vector2Int, Vector2Int> cameFrom`: Usado para rastrear o caminho. Cada entrada regista de qual nó anterior o inimigo veio para chegar ao nó atual.
* **Verificação de Obstáculos (`GetNeighbors`):** O método verifica os vizinhos (Cima, Baixo, Esquerda, Direita). Para determinar se um nó é válido, é usada a `Physics2D.OverlapCircle` para garantir que o centro da célula não está a colidir com a `obstacleLayer`.
* **Limite de Busca:** A variável `maxSearchSteps` atua como uma salvaguarda para evitar loops infinitos e proteger o desempenho, interrompendo a busca se o caminho for muito longo ou inacessível.

#### B. Reconstrução do Caminho (`ReconstructPath`)

Se o BFS for bem-sucedido, o `ReconstructPath` usa o dicionário `cameFrom` para traçar o caminho do nó final até ao nó inicial. O caminho é então armazenado numa lista (`currentPath`) e invertido para ser seguido na ordem correta.

#### C. Lógica de Movimento (`HandleChaseBFS` e `MoveTowardsNode`)

* **Recalculo:** O `HandleChaseBFS` executa o `RunBFS` periodicamente, controlado pela variável `pathUpdateRate` (tempo entre recalculos).
* **Seguimento:** O inimigo move-se sequencialmente para o próximo ponto na lista `currentPath`.
* **Salto:** A função `MoveTowardsNode` inclui lógica para detetar se o próximo nó está numa plataforma superior (verificando `targetPos.y > transform.position.y + 0.5f`) e aplica a `jumpForce` se o inimigo estiver no chão.

## 3. Lógica de Decisão: Smart Power-Up

Embora o conceito formal de *Decision Tree* (Árvore de Decisão) não esteja implementado, o script SmartPowerUp.cs codifica uma lógica de decisão baseada em prioridades e contexto, imitando a função de um sistema inteligente: otimizar o resultado para o jogador.

### 3.1 Priorização Contextual

O método `DecideEffect` decide o tipo de *power-up* a aplicar (Heal, DamageBoost, SpeedBoost) seguindo uma hierarquia de prioridades:

* **Sobrevivência (Heal):** Se a percentagem de vida do jogador for inferior ao `lowHealthThreshold` (ex: 30% ou 0.3f), a decisão é Cura total, independentemente do contexto.
* **Agressão (DamageBoost):** Se a sobrevivência não for crítica, verifica-se a proximidade de inimigos usando `IsEnemyNearby()` e um raio definido (`enemyCheckRadius`). Se houver inimigos, o aumento de Dano é priorizado para combater a ameaça imediata.
* **Exploração (SpeedBoost):** Se a sobrevivência não for crítica e não houver inimigos próximos, o SpeedBoost é aplicado, otimizando o tempo de viagem e a exploração do mapa.

### 3.2 Implementação da Lógica

* **Contexto de Vida:** Calculado com `(float)playerHealth.health / playerHealth.maxHealth`.
* **Contexto de Perigo (`IsEnemyNearby`):** Utiliza `Physics2D.OverlapCircle` com um raio (`enemyCheckRadius`) para verificar colisões dentro da Layer dos inimigos.
* **Rotinas de Buff:** Os *buffs* temporários (DamageBoost) são geridos por Corrotinas (`IEnumerator`) para aplicar o efeito, esperar pela `damageDuration` e, em seguida, reverter o dano para o valor original antes de destruir o objeto.

## Conclusão

O projeto ClashBound demonstra uma implementação bem-sucedida de conceitos cruciais de Inteligência Artificial para jogos *multiplayer*.

A escolha de arquiteturas reflete uma compreensão clara dos requisitos do motor e da rede:

* **Robustez Comportamental:** A utilização da **Máquina de Estados Finitos (FSM)** fornece um *framework* de comportamento claro e fiável para os inimigos, sendo o padrão da indústria.
* **Navegação Avançada:** A implementação do algoritmo **BFS (Pathfinding)** demonstra a capacidade da IA de planear caminhos e navegar em ambientes complexos de plataforma, indo além da simples perseguição em linha reta.
* **IA Aplicada à UX:** O **Smart Power-up** é um excelente exemplo de IA focada na *User Experience* (UX), onde o ambiente e o estado do jogador ditam o efeito de jogo, tornando a jogabilidade mais dinâmica e estratégica.
