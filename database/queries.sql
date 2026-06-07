-- SpaceStay - Parte 1: queries.sql, consultas SQL simulando o uso do sistema.
--
-- Pré-requisito: rodar schema.sql e seed.sql antes.
-- Cada consulta traz: (a) o que responde no negócio, (b) o SQL e (c) o resultado
-- esperado com os dados do seed.
--
-- As consultas 1 a 4 são as exigidas pelo enunciado da Parte 1; as demais são extras
-- que alimentam o app (painel "Mission Control", ocupação, etc.).
-- Requer MySQL 8.0+ / MariaDB 10.2+ (a consulta 3 usa window function).

USE spacestay;


-- 1) Alertas críticos em aberto, com o nome do módulo (consulta exigida pelo enunciado).
--    Negócio: feed prioritário da equipe, o que precisa de ação imediata. JOIN alerts com modules.
SELECT a.id,
       a.severity,
       a.message,
       a.created_at,
       m.name AS module_name
FROM alerts a
JOIN modules m ON m.id = a.module_id
WHERE a.status = 'open'
  AND a.severity = 'critical'
ORDER BY a.created_at DESC;
-- Resultado esperado: 1 linha.
--   id=1 | critical | "CO₂ em 2150 ppm na Cupola Suite 3 ..." | Cupola Suite 3


-- 2) Hóspedes atualmente em cada módulo, "buscar hóspedes por módulo" (consulta exigida).
--    Negócio: equivalente ao "buscar pessoas por abrigo" do enunciado. Só reservas active.
--    JOIN bookings com guests e modules.
SELECT m.id   AS module_id,
       m.name AS module_name,
       g.id   AS guest_id,
       g.name AS guest_name,
       b.check_in,
       b.check_out
FROM bookings b
JOIN guests  g ON g.id = b.guest_id
JOIN modules m ON m.id = b.module_id
WHERE b.status = 'active'
ORDER BY m.name, g.name;
-- Resultado esperado: 3 linhas.
--   Cupola Suite 3: Ana Beatriz Costa, Liam O'Connor
--   Aurora Suite 1: Kenji Tanaka

-- 2b) Variação "filtrar por um módulo específico" (o caso de uso direto da tela):
--     troque o module_id (aqui = 1) pelo módulo desejado.
SELECT g.name        AS guest_name,
       g.nationality,
       b.check_in,
       b.check_out
FROM bookings b
JOIN guests g ON g.id = b.guest_id
WHERE b.module_id = 1
  AND b.status = 'active';
-- Resultado esperado (módulo 1): Ana Beatriz Costa, Liam O'Connor


-- 3) Última leitura de cada sensor de um módulo, telemetria atual (consulta exigida).
--    Negócio: alimenta os medidores de conforto do hóspede e o detalhe do módulo para a
--    equipe. Usa window function para pegar a leitura mais recente por sensor (module_id = 1).
SELECT t.sensor_id,
       t.type,
       t.unit,
       t.value,
       t.recorded_at
FROM (
    SELECT s.id        AS sensor_id,
           s.type,
           s.unit,
           r.`value`   AS value,
           r.recorded_at,
           ROW_NUMBER() OVER (PARTITION BY s.id ORDER BY r.recorded_at DESC) AS rn
    FROM sensors s
    JOIN sensor_readings r ON r.sensor_id = s.id
    WHERE s.module_id = 1
) AS t
WHERE t.rn = 1
ORDER BY t.type;
-- Resultado esperado (módulo 1): 6 linhas, a leitura mais recente de cada sensor.
--   co2=2150.00 | humidity=46.00 | o2=20.90 | pressure=101.30 | temperature=28.50 | water=74.00


-- 4) Quantidade de excursões reservadas por hóspede (consulta exigida).
--    Negócio: visão comercial das atividades. LEFT JOIN para incluir quem tem 0.
--    Desconsidera reservas canceladas. guests LEFT JOIN excursion_bookings + GROUP BY.
SELECT g.id,
       g.name,
       COUNT(eb.id) AS total_excursoes
FROM guests g
LEFT JOIN excursion_bookings eb
       ON eb.guest_id = g.id
      AND eb.status <> 'cancelled'
GROUP BY g.id, g.name
ORDER BY total_excursoes DESC, g.name;
-- Resultado esperado:
--   Ana Beatriz Costa = 3 | Kenji Tanaka = 2 | Liam O'Connor = 1 | Noah Schmidt = 1 | Sofia Rossi = 0


-- 5) Painel "Mission Control": módulos, status e número de alertas em aberto.
--    Negócio: a tela inicial da equipe (Parte 4). LEFT JOIN para incluir módulos sem alerta.
SELECT m.id,
       m.name,
       m.type,
       m.status,
       COUNT(a.id) AS alertas_abertos
FROM modules m
LEFT JOIN alerts a ON a.module_id = m.id AND a.status = 'open'
GROUP BY m.id, m.name, m.type, m.status
ORDER BY alertas_abertos DESC, m.name;
-- Resultado esperado: Cupola Suite 3 = 1, Aurora Suite 1 = 1


-- 6) Leituras fora da faixa segura (a regra que dispara alertas, Parte 6).
--    Negócio: mostra exatamente quais medições violaram os limites do sensor.
SELECT m.name AS module_name,
       s.type,
       r.`value` AS valor,
       s.unit,
       s.min_safe,
       s.max_safe,
       r.recorded_at
FROM sensor_readings r
JOIN sensors s ON s.id = r.sensor_id
JOIN modules m ON m.id = s.module_id
WHERE r.`value` < s.min_safe
   OR r.`value` > s.max_safe
ORDER BY r.recorded_at;
-- Resultado esperado: 3 linhas.
--   Cupola Suite 3 | co2  | 2150.00 (max 1000)
--   Cupola Suite 3 | temperature | 28.50 (max 27)
--   Aurora Suite 1 | water | 18.00 (min 20)


-- 7) Ocupação atual por módulo (vagas disponíveis).
--    Negócio: regra de capacidade usada ao criar reserva (Parte 2).
SELECT m.id,
       m.name,
       m.capacity,
       COUNT(b.id)               AS ocupados_agora,
       m.capacity - COUNT(b.id)  AS vagas
FROM modules m
LEFT JOIN bookings b ON b.module_id = m.id AND b.status = 'active'
GROUP BY m.id, m.name, m.capacity
ORDER BY m.name;
-- Resultado esperado:
--   Aurora Suite 1 (cap 2): 1 ocupado, 1 vaga
--   Cupola Suite 3 (cap 2): 2 ocupados, 0 vaga


-- 8) Alertas tratados por membro da equipe (histórico de atuação).
--    Negócio: auditoria de quem reconheceu/resolveu alertas (RBAC, Parte 5).
SELECT st.name AS staff_name,
       st.role,
       COUNT(a.id) AS alertas_tratados
FROM alerts a
JOIN staff st ON st.id = a.resolved_by
WHERE a.status IN ('acknowledged', 'resolved')
GROUP BY st.id, st.name, st.role
ORDER BY alertas_tratados DESC, st.name;
-- Resultado esperado: Rafael Lima (engineer) = 1, Yuri Petrov (engineer) = 1

-- Fim das consultas.
