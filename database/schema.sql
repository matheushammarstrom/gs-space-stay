-- SpaceStay - Parte 1: modelo de banco de dados (DDL: tabelas, PKs, FKs, constraints).
-- Alvo: MySQL 8.0+ / MariaDB 10.2+ (InnoDB, utf8mb4). Usa ENUM nativo nos domínios
-- fechados e CHECK constraints; os tipos mapeiam direto nos modelos EF Core da Parte 2.
--
-- Domínio: operação de um hotel orbital. Gerencia hospedagens e monitora o suporte de
-- vida de cada módulo (O₂, CO₂, pressão, temperatura, umidade, água). Quando uma leitura
-- sai da faixa segura, o sistema gera um alerta para a equipe agir.
--
-- Executar: mysql -u root -p < schema.sql

CREATE DATABASE IF NOT EXISTS spacestay
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE spacestay;

-- Recriação idempotente: derruba as tabelas na ordem inversa das dependências para o
-- script poder rodar de novo sem erro de FK.
SET FOREIGN_KEY_CHECKS = 0;
DROP TABLE IF EXISTS excursion_bookings;
DROP TABLE IF EXISTS alerts;
DROP TABLE IF EXISTS sensor_readings;
DROP TABLE IF EXISTS sensors;
DROP TABLE IF EXISTS bookings;
DROP TABLE IF EXISTS excursions;
DROP TABLE IF EXISTS modules;
DROP TABLE IF EXISTS staff;
DROP TABLE IF EXISTS guests;
SET FOREIGN_KEY_CHECKS = 1;


-- 1) guests: hóspedes do hotel orbital (cadastram, reservam um módulo e fazem excursões).
CREATE TABLE guests (
    id                INT          NOT NULL AUTO_INCREMENT,
    name              VARCHAR(120) NOT NULL,                 -- nome completo do hóspede
    email             VARCHAR(180) NOT NULL,                 -- usado no login (único)
    password_hash     VARCHAR(255) NOT NULL,                 -- nunca senha em texto puro (ver Parte 5)
    nationality       VARCHAR(60)  NULL,                     -- nacionalidade (opcional)
    medical_clearance BOOLEAN      NOT NULL DEFAULT FALSE,   -- liberação médica para voo orbital
    created_at        DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT pk_guests       PRIMARY KEY (id),
    CONSTRAINT uq_guests_email UNIQUE (email)                -- não pode haver e-mail repetido
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;


-- 2) staff: equipe interna. O papel (role) controla o que cada um pode fazer (RBAC, Parte 5).
CREATE TABLE staff (
    id            INT          NOT NULL AUTO_INCREMENT,
    name          VARCHAR(120) NOT NULL,
    email         VARCHAR(180) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    role          ENUM('admin', 'engineer', 'concierge') NOT NULL,  -- perfil de acesso
    created_at    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT pk_staff       PRIMARY KEY (id),
    CONSTRAINT uq_staff_email UNIQUE (email)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;


-- 3) modules: os "quartos" (cápsulas seladas). type: suite/common/lab; status: available/occupied/maintenance.
CREATE TABLE modules (
    id       INT         NOT NULL AUTO_INCREMENT,
    name     VARCHAR(80) NOT NULL,                            -- ex.: "Cupola Suite 3"
    type     ENUM('suite', 'common', 'lab')                       NOT NULL,
    capacity INT         NOT NULL,                            -- lotação máxima de hóspedes
    status   ENUM('available', 'occupied', 'maintenance')        NOT NULL DEFAULT 'available',

    CONSTRAINT pk_modules           PRIMARY KEY (id),
    CONSTRAINT uq_modules_name      UNIQUE (name),
    CONSTRAINT chk_modules_capacity CHECK (capacity > 0)      -- capacidade sempre positiva
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;


-- 4) excursions: atividades ofertadas aos hóspedes (ex.: caminhada espacial, observação da Terra).
CREATE TABLE excursions (
    id           INT          NOT NULL AUTO_INCREMENT,
    name         VARCHAR(100) NOT NULL,
    description  VARCHAR(500) NULL,
    capacity     INT          NOT NULL,                       -- vagas da excursão
    scheduled_at DATETIME     NOT NULL,                       -- data/hora agendada

    CONSTRAINT pk_excursions           PRIMARY KEY (id),
    CONSTRAINT chk_excursions_capacity CHECK (capacity > 0)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;


-- 5) bookings: reservas de hospedagem. Relação N:N entre guests e modules, com período e status.
CREATE TABLE bookings (
    id        INT      NOT NULL AUTO_INCREMENT,
    guest_id  INT      NOT NULL,
    module_id INT      NOT NULL,
    check_in  DATETIME NOT NULL,                              -- início da estadia
    check_out DATETIME NOT NULL,                              -- fim da estadia
    status    ENUM('confirmed', 'active', 'completed')            NOT NULL DEFAULT 'confirmed',

    CONSTRAINT pk_bookings        PRIMARY KEY (id),
    -- Se o hóspede for removido, suas reservas vão junto (CASCADE).
    CONSTRAINT fk_bookings_guest  FOREIGN KEY (guest_id)
        REFERENCES guests (id)  ON DELETE CASCADE  ON UPDATE CASCADE,
    -- Não permite apagar um módulo com reservas (RESTRICT), preservando o histórico.
    CONSTRAINT fk_bookings_module FOREIGN KEY (module_id)
        REFERENCES modules (id) ON DELETE RESTRICT ON UPDATE CASCADE,
    -- Data de saída tem que ser depois da entrada.
    CONSTRAINT chk_bookings_dates CHECK (check_out > check_in),

    -- Índices para as consultas frequentes (hóspedes ativos por módulo, reservas do hóspede).
    INDEX ix_bookings_module_status (module_id, status),
    INDEX ix_bookings_guest         (guest_id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;


-- 6) sensors: sensores IoT de cada módulo. min_safe/max_safe definem a faixa segura da regra de alerta.
CREATE TABLE sensors (
    id        INT          NOT NULL AUTO_INCREMENT,
    module_id INT          NOT NULL,
    type      ENUM('o2', 'co2', 'pressure', 'temperature', 'humidity', 'water') NOT NULL,
    unit      VARCHAR(12)  NOT NULL,                          -- ex.: '%', 'ppm', 'kPa', '°C', '%RH'
    min_safe  DECIMAL(8,2) NOT NULL,                          -- limite inferior seguro
    max_safe  DECIMAL(8,2) NOT NULL,                          -- limite superior seguro

    CONSTRAINT pk_sensors        PRIMARY KEY (id),
    CONSTRAINT fk_sensors_module FOREIGN KEY (module_id)
        REFERENCES modules (id) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT chk_sensors_range CHECK (max_safe > min_safe), -- faixa coerente
    -- Cada módulo tem no máximo um sensor de cada tipo.
    CONSTRAINT uq_sensors_module_type UNIQUE (module_id, type)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;


-- 7) sensor_readings: leituras de telemetria. Alto volume, por isso a PK é BIGINT.
CREATE TABLE sensor_readings (
    id          BIGINT       NOT NULL AUTO_INCREMENT,
    sensor_id   INT          NOT NULL,
    `value`     DECIMAL(8,2) NOT NULL,                        -- valor medido
    recorded_at DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT pk_sensor_readings    PRIMARY KEY (id),
    CONSTRAINT fk_readings_sensor    FOREIGN KEY (sensor_id)
        REFERENCES sensors (id) ON DELETE CASCADE ON UPDATE CASCADE,

    -- Índice composto para "última leitura por sensor" e séries temporais.
    INDEX ix_readings_sensor_time (sensor_id, recorded_at)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;


-- 8) alerts: gerados quando uma leitura sai da faixa segura. resolved_by aponta quem tratou (pode ser NULL).
CREATE TABLE alerts (
    id          INT          NOT NULL AUTO_INCREMENT,
    sensor_id   INT          NOT NULL,
    module_id   INT          NOT NULL,
    severity    ENUM('info', 'warning', 'critical')              NOT NULL,
    message     VARCHAR(255) NOT NULL,                        -- mensagem legível para a equipe
    status      ENUM('open', 'acknowledged', 'resolved')         NOT NULL DEFAULT 'open',
    created_at  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    resolved_by INT          NULL,                            -- quem tratou (FK opcional para staff)

    CONSTRAINT pk_alerts        PRIMARY KEY (id),
    CONSTRAINT fk_alerts_sensor FOREIGN KEY (sensor_id)
        REFERENCES sensors (id) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_alerts_module FOREIGN KEY (module_id)
        REFERENCES modules (id) ON DELETE CASCADE ON UPDATE CASCADE,
    -- Se o membro da equipe for removido, o alerta permanece, mas perde o vínculo.
    CONSTRAINT fk_alerts_staff  FOREIGN KEY (resolved_by)
        REFERENCES staff (id)   ON DELETE SET NULL ON UPDATE CASCADE,

    -- Índices para o feed de alertas (filtro por status e por módulo).
    INDEX ix_alerts_status (status),
    INDEX ix_alerts_module (module_id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;


-- 9) excursion_bookings: tabela de junção N:N entre guests e excursions.
CREATE TABLE excursion_bookings (
    id           INT NOT NULL AUTO_INCREMENT,
    guest_id     INT NOT NULL,
    excursion_id INT NOT NULL,
    status       ENUM('booked', 'cancelled', 'attended')         NOT NULL DEFAULT 'booked',

    CONSTRAINT pk_excursion_bookings PRIMARY KEY (id),
    CONSTRAINT fk_exbk_guest     FOREIGN KEY (guest_id)
        REFERENCES guests (id)     ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_exbk_excursion FOREIGN KEY (excursion_id)
        REFERENCES excursions (id) ON DELETE CASCADE ON UPDATE CASCADE,
    -- Um hóspede reserva a mesma excursão no máximo uma vez.
    CONSTRAINT uq_exbk_guest_excursion UNIQUE (guest_id, excursion_id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

-- Fim do schema. Próximos arquivos: seed.sql (dados de exemplo) e queries.sql.
