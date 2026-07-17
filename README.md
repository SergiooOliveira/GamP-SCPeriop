# L'Ermite's Run

## Game Concept
*L'Ermite's Run* is an endless dungeon crawler disguised as a dark, drag-and-drop Solitaire game. You play as a solitary Hermit navigating a shadowy crypt by stacking cards. It combines the familiar puzzle-solving of Solitaire with tense, survival-RPG resource management. Weapons and armor are persistent physical cards that degrade with every hit, forcing agonizing tactical choices as you fight to escape a crypt that is constantly collapsing around you.

## Gameplay Loop
* **The Deal:** The crypt is dealt into standard columns. Top cards are illuminated; the rest are in the shadows.
* **Organize & Reveal:** Drag and drop illuminated cards to stack them in descending order and alternating colors. This organizes your board and reveals the hidden cards beneath.
* **Navigate Blockers:** Monsters (Spades/Clubs) are solid objects. You cannot stack cards on top of them, forcing you to build around them or fight them to clear the column.
* **Engage (RPG Combat):** Drag exposed cards to your Hermit Slot to consume them. 
    * *Clubs/Diamonds:* Equip as Weapons/Armor. You cannot unequip them; you must shatter them in combat to free up the slot.
    * *Spades:* Fight monsters. Damage chips away your Weapon, then Armor, then HP.
    * *Hearts:* Consume to heal HP.
* **Survive the Doom:** Every 5 successful moves, the crypt shifts, dealing a new card to the top of every column. If a column exceeds 6 cards, it overflows—the bottom card falls off the screen and auto-triggers its effect on the Hermit.
* **Escape:** Move cards to Foundation piles to earn Gold. Clearing the board plunges you into a new, harder floor.

## Technical Approach
* **Framework:** 100% 2D SpriteKit using Swift. 
* **Input Handling:** Custom drag-and-drop by overriding `touchesBegan`, `touchesMoved`, and `touchesEnded` in the main `SKScene`.
* **Dual Hit-Testing:** Touch logic checks for two distinct drop zones: Column/Foundation drops (validates Solitaire math) and Hermit drops (triggers RPG math and deletes the node).
* **Data-Driven Architecture:** SpriteKit physics/collisions will *not* be used to govern rules. The game state is strictly controlled by pure Swift arrays and integer pools. A successful drop updates the arrays first, and then triggers visual `SKAction` movements and label updates.
* **Dynamic UI:** Because cards act as degrading health pools, `SKSpriteNode` cards will feature child `SKLabelNodes` that dynamically update their text during combat calculations.

## MVP Scope
* **Week 1 (Foundation):** Build pure Swift data models (Card structs, Deck, Column arrays). Render basic card nodes on screen.
* **Week 2 (Drag System):** Implement touch tracking. Ensure a dragged card snaps back to its original position if dropped in an invalid space.
* **Week 3 (Solitaire Rules):** Implement valid column hit-testing, stack-dragging, and the "Blocker" rule that rejects drops onto Black suits.
* **Week 4 (Persistent Math):** Build the Hermit slot. Implement the combat math (Weapon blocks first, then Armor, then HP) and the "No Overwrite" equipment rule. Add the Game Over state.
* **Week 5 (The Crypt Shifts):** Implement the 5-move turn counter. Write the logic that appends a new card to every column, and the Overflow logic that damages the player if a column exceeds 6 cards. 
* **Week 6 (The Loop):** Implement Foundation piles. Write the logic that detects a cleared board, increments difficulty, and instantly deals a new floor.

## Future Ideas
* **Joker Assassinations:** Add two Jokers to the deck that can be dragged onto any card to instantly destroy it with zero negative effects.
* **The Hermit's Shop:** Implement a store between floors where players can spend earned Gold to purchase permanent max HP upgrades, permanent damage buff, guarantee a shield at the start of the next run.
* **Visual Polish:** Add particle emitters (`SKEmitterNode`) for when a weapon shatters or a potion is consumed.

---

# L'Ermite's Run (PT-PT)

## Conceito do Jogo
*L'Ermite's Run* é um *dungeon crawler* infinito disfarçado de um jogo de solitário sombrio do tipo *drag-and-drop* (arrastar e largar). Jogas como um Eremita solitário que navega por uma cripta sombria empilhando cartas. O jogo combina a resolução de puzzles familiar do solitário com a gestão de recursos tensa de um RPG de sobrevivência. Armas e armaduras são cartas físicas persistentes que se degradam a cada golpe, forçando escolhas táticas agonizantes enquanto lutas para escapar de uma cripta que desmorona constantemente à tua volta.

## Ciclo de Jogabilidade (Gameplay Loop)
* **A Distribuição:** A cripta é distribuída em colunas padrão. As cartas do topo estão iluminadas; as restantes estão nas sombras.
* **Organizar e Revelar:** Arrasta e larga cartas iluminadas para as empilhar por ordem decrescente e com cores alternadas. Isto organiza o teu tabuleiro e revela as cartas escondidas por baixo.
* **Navegar Obstáculos:** Monstros (Espadas/Paus) são objetos sólidos. Não podes empilhar cartas em cima deles, forçando-te a construir à sua volta ou a combatê-los para limpar a coluna.
* **Combate RPG:** Arrasta cartas expostas para o teu Espaço de Eremita (*Hermit Slot*) para as consumires. 
    * *Paus/Ouros:* Equipa como Armas/Armadura. Não podes desequipá-las; tens de as despedaçar em combate para libertar o espaço.
    * *Espadas:* Combate monstros. O dano desgasta a tua Arma, depois a Armadura, e depois os teus HP (Pontos de Vida).
    * *Copas:* Consome para curar HP.
* **Sobreviver à Ruína:** A cada 5 movimentos bem-sucedidos, a cripta altera-se, distribuindo uma nova carta para o topo de cada coluna. Se uma coluna exceder 6 cartas, ela transborda — a carta de baixo cai do ecrã e ativa automaticamente o seu efeito no Eremita.
* **Escapar:** Move as cartas para as pilhas de Fundação para ganhar Ouro. Limpar o tabuleiro atira-te para um andar novo e mais difícil.

## Abordagem Técnica
* **Framework:** 100% 2D SpriteKit usando Swift. 
* **Gestão de Inputs:** *Drag-and-drop* personalizado através da sobreposição (*override*) de `touchesBegan`, `touchesMoved` e `touchesEnded` na `SKScene` principal.
* **Teste de Colisão Duplo:** A lógica de toque verifica duas zonas de largada distintas: Colunas/Fundação (valida a matemática do Solitário) e Eremita (ativa a matemática do RPG e elimina o *node* da carta).
* **Arquitetura Orientada a Dados:** A física e as colisões do SpriteKit *não* serão usadas para ditar as regras. O estado do jogo é estritamente controlado por *arrays* puros de Swift e *pools* de inteiros. Uma largada bem-sucedida atualiza primeiro os *arrays* e depois aciona os movimentos visuais `SKAction` e as atualizações de *labels*.
* **UI Dinâmica:** Como as cartas funcionam como reservas de vida que se degradam, as cartas `SKSpriteNode` terão `SKLabelNodes` filhos que atualizam dinamicamente o seu texto durante os cálculos de combate.

## Âmbito do MVP
* **Semana 1 (Fundação):** Criar modelos de dados puros em Swift (*structs* de Cartas, Baralho, *arrays* de Colunas). Renderizar *nodes* básicos de cartas no ecrã.
* **Semana 2 (Sistema de Arrastar):** Implementar rastreamento de toques. Garantir que uma carta arrastada volta à sua posição original se for largada num espaço inválido.
* **Semana 3 (Regras do Solitário):** Implementar testes de colisão válidos nas colunas, arrastar de pilhas, e a regra de "Obstáculo" que rejeita largadas em naipes Pretos.
* **Semana 4 (Matemática Persistente):** Construir o espaço do Eremita. Implementar a matemática de combate (a Arma bloqueia primeiro, depois a Armadura, e depois os HP) e a regra de equipamento "Sem Substituição". Adicionar o estado de *Game Over*.
* **Semana 5 (A Cripta Altera-se):** Implementar o contador de turnos de 5 movimentos. Escrever a lógica que adiciona uma nova carta a cada coluna, e a lógica de Transbordo (*Overflow*) que causa dano ao jogador se uma coluna exceder as 6 cartas. 
* **Semana 6 (O Ciclo):** Implementar as pilhas de Fundação. Escrever a lógica que deteta um tabuleiro limpo, incrementa a dificuldade e distribui instantaneamente um novo andar.

## Ideias Futuras
* **Assassinatos com Jokers:** Adicionar dois Jokers ao baralho que podem ser arrastados para qualquer carta para a destruir instantaneamente com zero efeitos negativos.
* **A Loja do Eremita:** Implementar uma loja entre andares onde os jogadores podem gastar o Ouro ganho para comprar melhorias permanentes de HP máximo, *buffs* de dano permanentes, ou garantir um escudo no início da próxima *run*.
* **Polimento Visual:** Adicionar emissores de partículas (`SKEmitterNode`) para quando uma arma se despedaça ou uma poção é consumida.
