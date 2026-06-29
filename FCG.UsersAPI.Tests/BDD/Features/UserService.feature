Feature: Gerenciamento de Usuários
  Como sistema de gerenciamento de usuários
  Quero registrar e gerenciar usuários
  Para que possam acessar a plataforma FCG Games

  # ---- RegisterAsync ----

  Scenario: Registrar usuário com dados inválidos retorna falha
    When o sistema tenta registrar um usuário com dados inválidos
    Then o resultado de registro deve ser falha com mensagem "Invalid request data"

  Scenario: Registrar usuário com e-mail já cadastrado retorna falha
    Given já existe um usuário cadastrado com o e-mail "existente@fcg.com"
    When o sistema tenta registrar um usuário com o mesmo e-mail "existente@fcg.com"
    Then o resultado de registro deve ser falha com mensagem "Usuário já cadastrado"

  Scenario: Registrar usuário com dados válidos retorna sucesso
    When o sistema registra um usuário com nome "João Silva" e-mail "joao@fcg.com" e senha "Senha@123"
    Then o resultado de registro deve ser sucesso
    And o usuário retornado deve ter nome "João Silva"
    And o usuário retornado deve ter e-mail "joao@fcg.com"

  Scenario: Registrar usuário com dados válidos publica evento de criação
    When o sistema registra um usuário com nome "Maria Santos" e-mail "maria@fcg.com" e senha "Senha@123"
    Then o evento de criação de usuário deve ter sido publicado

  Scenario: Registrar usuário com dados válidos chama repositório para adicionar
    When o sistema registra um usuário com nome "Carlos Souza" e-mail "carlos@fcg.com" e senha "Senha@123"
    Then o repositório deve ter sido chamado para adicionar o usuário

  # ---- GetAll ----

  Scenario: Listar todos os usuários retorna a lista completa
    Given 3 usuários cadastrados no sistema
    When todos os usuários são listados
    Then devem ser retornados 3 usuários

  Scenario: Listar usuários com repositório vazio retorna lista vazia
    Given nenhum usuário cadastrado no sistema
    When todos os usuários são listados
    Then devem ser retornados 0 usuários

  # ---- GetById ----

  Scenario: Buscar usuário por id existente retorna o usuário
    Given um usuário cadastrado no sistema
    When o sistema busca o usuário pelo id cadastrado
    Then o usuário deve ser retornado com sucesso

  Scenario: Buscar usuário por id inexistente retorna nulo
    When o sistema busca um usuário por id inexistente
    Then o resultado deve ser nulo

  # ---- Update ----

  Scenario: Atualizar usuário inexistente retorna nulo
    When o sistema tenta atualizar um usuário com id inexistente
    Then o resultado deve ser nulo

  Scenario: Atualizar usuário existente com novo nome
    Given um usuário cadastrado no sistema
    When o sistema atualiza o usuário com o nome "Novo Nome Atualizado"
    Then o usuário deve ter nome "Novo Nome Atualizado"
    And o repositório deve ter sido chamado para atualizar o usuário

  Scenario: Desativar usuário existente
    Given um usuário cadastrado no sistema
    When o sistema desativa o usuário
    Then o usuário deve estar inativo
