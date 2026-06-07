-- SpaceStay - Parte 1: seed.sql, dados de exemplo para demonstração.
--
-- Objetivo: popular o banco com um cenário coerente que deixa a API (Parte 2) e o app
-- (Parte 4) demonstráveis de imediato, já preparando o destaque do pitch: um alerta
-- crítico de CO₂ em aberto.
--
-- Pré-requisito: rodar antes o schema.sql.
-- Executar: mysql -u root -p spacestay < seed.sql
--
-- Observações:
--   Os IDs são informados explicitamente para deixar as FKs previsíveis e os resultados
--   das consultas (queries.sql) determinísticos.
--   O password_hash aqui é um valor ilustrativo. Na prática a API (Parte 5) gera o hash
--   real com PBKDF2 no cadastro; no seed é só para preencher a coluna.
--   As datas usam "hoje = 2026-06-02" para o cenário fazer sentido.

USE spacestay;

-- Limpa os dados anteriores (mantém a estrutura). TRUNCATE precisa das FKs desligadas.
SET FOREIGN_KEY_CHECKS = 0;
TRUNCATE TABLE excursion_bookings;
TRUNCATE TABLE alerts;
TRUNCATE TABLE sensor_readings;
TRUNCATE TABLE sensors;
TRUNCATE TABLE bookings;
TRUNCATE TABLE excursions;
TRUNCATE TABLE modules;
TRUNCATE TABLE staff;
TRUNCATE TABLE guests;
SET FOREIGN_KEY_CHECKS = 1;


-- guests: hóspedes (o hotel orbital é internacional)
INSERT INTO guests (id, name, email, password_hash, nationality, medical_clearance, created_at) VALUES
    (1, 'Ana Beatriz Costa', 'ana.costa@example.com',    '$2a$11$Vega0rbitDEMOsaltxxxxxuP9Qk2mJ7nFhq3sLbcD1eFgH2iJkLmN', 'Brasil',    TRUE,  '2026-05-20 09:15:00'),
    (2, 'Kenji Tanaka',      'kenji.tanaka@example.com',  '$2a$11$Vega0rbitDEMOsaltxxxxxuP9Qk2mJ7nFhq3sLbcD1eFgH2iJkLmN', 'Japão',     TRUE,  '2026-05-21 14:40:00'),
    (3, 'Liam O''Connor',    'liam.oconnor@example.com',  '$2a$11$Vega0rbitDEMOsaltxxxxxuP9Qk2mJ7nFhq3sLbcD1eFgH2iJkLmN', 'Irlanda',   TRUE,  '2026-05-22 11:05:00'),
    (4, 'Sofia Rossi',       'sofia.rossi@example.com',   '$2a$11$Vega0rbitDEMOsaltxxxxxuP9Qk2mJ7nFhq3sLbcD1eFgH2iJkLmN', 'Itália',    FALSE, '2026-05-25 18:20:00'),
    (5, 'Noah Schmidt',      'noah.schmidt@example.com',  '$2a$11$Vega0rbitDEMOsaltxxxxxuP9Qk2mJ7nFhq3sLbcD1eFgH2iJkLmN', 'Alemanha',  TRUE,  '2026-05-08 08:00:00');


-- staff: equipe (perfis admin, engineer, concierge)
INSERT INTO staff (id, name, email, password_hash, role, created_at) VALUES
    (1, 'Helena Vasquez', 'helena.vasquez@spacestay.space', '$2a$11$Vega0rbitDEMOsaltxxxxxuP9Qk2mJ7nFhq3sLbcD1eFgH2iJkLmN', 'admin',     '2026-04-01 08:00:00'),
    (2, 'Rafael Lima',    'rafael.lima@spacestay.space',    '$2a$11$Vega0rbitDEMOsaltxxxxxuP9Qk2mJ7nFhq3sLbcD1eFgH2iJkLmN', 'engineer',  '2026-04-01 08:00:00'),
    (3, 'Sofia Mendes',   'sofia.mendes@spacestay.space',   '$2a$11$Vega0rbitDEMOsaltxxxxxuP9Qk2mJ7nFhq3sLbcD1eFgH2iJkLmN', 'concierge', '2026-04-02 09:30:00'),
    (4, 'Yuri Petrov',    'yuri.petrov@spacestay.space',    '$2a$11$Vega0rbitDEMOsaltxxxxxuP9Qk2mJ7nFhq3sLbcD1eFgH2iJkLmN', 'engineer',  '2026-04-05 10:00:00');


-- modules: os "quartos" / cápsulas (2 suítes)
INSERT INTO modules (id, name, type, capacity, status) VALUES
    (1, 'Cupola Suite 3', 'suite', 2, 'occupied'),   -- suíte com 2 hóspedes ativos
    (2, 'Aurora Suite 1', 'suite', 2, 'occupied');   -- suíte com 1 hóspede ativo


-- excursions: atividades ofertadas
INSERT INTO excursions (id, name, description, capacity, scheduled_at) VALUES
    (1, 'Caminhada Espacial (EVA)',      'Atividade extraveicular guiada com traje pressurizado e vista direta da órbita.', 4,  '2026-06-08 14:00:00'),
    (2, 'Observação da Terra na Cúpula', 'Sessão contemplativa na cúpula panorâmica durante o nascer do Sol orbital.',      8,  '2026-06-05 06:30:00'),
    (3, 'Jantar em Gravidade Zero',      'Experiência gastronômica em microgravidade com chef convidado.',                  12, '2026-06-07 20:00:00'),
    (4, 'Sessão de Astrofotografia',     'Captura de imagens do céu profundo com o telescópio da estação.',                 6,  '2026-06-09 23:00:00');


-- bookings: reservas de hospedagem.
--   Módulo 1 (cap. 2): Ana e Liam ativos (lotado), Noah já encerrado.
--   Módulo 2 (cap. 2): Kenji ativo.
INSERT INTO bookings (id, guest_id, module_id, check_in, check_out, status) VALUES
    (1, 1, 1, '2026-05-30 16:00:00', '2026-06-06 11:00:00', 'active'),
    (2, 3, 1, '2026-06-01 16:00:00', '2026-06-05 11:00:00', 'active'),
    (3, 2, 2, '2026-05-28 16:00:00', '2026-06-04 11:00:00', 'active'),
    (4, 5, 1, '2026-05-10 16:00:00', '2026-05-17 11:00:00', 'completed');


-- sensors: 6 sensores por módulo (faixas seguras de referência).
--   Tipos: o2, co2, pressure, temperature, humidity, water.
--   IDs: módulo 1 = 1..6, módulo 2 = 7..12.
INSERT INTO sensors (id, module_id, type, unit, min_safe, max_safe) VALUES
    -- Módulo 1: Cupola Suite 3
    (1,  1, 'o2',          '%',    19.50,  23.50),
    (2,  1, 'co2',         'ppm',   0.00, 1000.00),
    (3,  1, 'pressure',    'kPa',  95.00,  105.00),
    (4,  1, 'temperature', '°C',   18.00,   27.00),
    (5,  1, 'humidity',    '%RH',  30.00,   60.00),
    (6,  1, 'water',       '%',    20.00,  100.00),
    -- Módulo 2: Aurora Suite 1
    (7,  2, 'o2',          '%',    19.50,   23.50),
    (8,  2, 'co2',         'ppm',   0.00, 1000.00),
    (9,  2, 'pressure',    'kPa',  95.00,  105.00),
    (10, 2, 'temperature', '°C',   18.00,   27.00),
    (11, 2, 'humidity',    '%RH',  30.00,   60.00),
    (12, 2, 'water',       '%',    20.00,  100.00);


-- sensor_readings: leituras de telemetria.
--   Módulo 1: as últimas leituras de CO₂ (2150 ppm) e temperatura (28.5 °C) estão fora
--   da faixa segura e justificam os alertas abaixo.
--   Módulo 2: a última de água (18%) está abaixo de 20%, gerando alerta.
INSERT INTO sensor_readings (sensor_id, `value`, recorded_at) VALUES
    -- Módulo 1: O₂ (seguro)
    (1, 20.90, '2026-06-02 08:00:00'), (1, 20.80, '2026-06-02 09:00:00'), (1, 20.90, '2026-06-02 10:00:00'),
    -- Módulo 1: CO₂ (sobe até crítico)
    (2, 620.00, '2026-06-02 08:00:00'), (2, 880.00, '2026-06-02 09:00:00'), (2, 2150.00, '2026-06-02 10:00:00'),
    -- Módulo 1: Pressão (seguro)
    (3, 101.30, '2026-06-02 08:00:00'), (3, 101.20, '2026-06-02 09:00:00'), (3, 101.30, '2026-06-02 10:00:00'),
    -- Módulo 1: Temperatura (última fora da faixa: 28.5 > 27)
    (4, 22.00, '2026-06-02 08:00:00'), (4, 22.50, '2026-06-02 09:00:00'), (4, 28.50, '2026-06-02 10:00:00'),
    -- Módulo 1: Umidade (seguro)
    (5, 45.00, '2026-06-02 08:00:00'), (5, 47.00, '2026-06-02 09:00:00'), (5, 46.00, '2026-06-02 10:00:00'),
    -- Módulo 1: Água (seguro)
    (6, 78.00, '2026-06-02 08:00:00'), (6, 76.00, '2026-06-02 09:00:00'), (6, 74.00, '2026-06-02 10:00:00'),

    -- Módulo 2: O₂ / CO₂ / pressão / temperatura / umidade (seguros)
    (7,  20.70, '2026-06-02 09:30:00'), (7,  20.60, '2026-06-02 10:30:00'),
    (8,  540.00,'2026-06-02 09:30:00'), (8,  610.00,'2026-06-02 10:30:00'),
    (9,  100.80,'2026-06-02 09:30:00'), (9,  100.90,'2026-06-02 10:30:00'),
    (10, 21.50, '2026-06-02 09:30:00'), (10, 21.80, '2026-06-02 10:30:00'),
    (11, 52.00, '2026-06-02 09:30:00'), (11, 55.00, '2026-06-02 10:30:00'),
    -- Módulo 2: Água (última abaixo do mínimo: 18 < 20)
    (12, 22.00, '2026-06-02 09:30:00'), (12, 18.00, '2026-06-02 10:30:00');


-- alerts: gerados pela regra "leitura fora da faixa segura".
--   alert 1: crítico em aberto (CO₂ 2150 ppm), o destaque do pitch.
--   alert 2: warning já reconhecido (temperatura), tratado pelo eng. Rafael.
--   alert 3: warning em aberto (água baixa no módulo 2).
--   alert 4: warning resolvido (histórico), tratado pelo eng. Yuri.
INSERT INTO alerts (id, sensor_id, module_id, severity, message, status, created_at, resolved_by) VALUES
    (1, 2,  1, 'critical', 'CO₂ em 2150 ppm na Cupola Suite 3: nível crítico, verificar o depurador de CO₂.', 'open',         '2026-06-02 10:00:05', NULL),
    (2, 4,  1, 'warning',  'Temperatura em 28.5 °C na Cupola Suite 3: acima da faixa de conforto.',           'acknowledged', '2026-06-02 10:00:07', 2),
    (3, 12, 2, 'warning',  'Reserva de água em 18% na Aurora Suite 1: abaixo do mínimo, reabastecer.',        'open',         '2026-06-02 10:30:04', NULL),
    (4, 8,  2, 'warning',  'CO₂ em 1120 ppm na Aurora Suite 1: acima do ideal (já normalizado).',             'resolved',     '2026-06-01 22:10:00', 4);


-- excursion_bookings: reservas de excursões (N:N entre guests e excursions).
--   Ana: 3 excursões, Kenji: 2, Liam: 1, Noah: 1 (compareceu), Sofia: 0.
INSERT INTO excursion_bookings (id, guest_id, excursion_id, status) VALUES
    (1, 1, 1, 'booked'),
    (2, 1, 2, 'booked'),
    (3, 1, 3, 'booked'),
    (4, 2, 1, 'booked'),
    (5, 2, 4, 'booked'),
    (6, 3, 2, 'booked'),
    (7, 5, 3, 'attended');

-- Fim do seed. As consultas de exemplo estão em queries.sql.
