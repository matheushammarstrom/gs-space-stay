namespace SpaceStay.Core.Domain;

// Os membros ficam em minúsculo de propósito: são gravados como string e precisam
// casar com os valores das colunas ENUM do MySQL (ver database/schema.sql).

public enum StaffRole { admin, engineer, concierge }
public enum ModuleType { suite, common, lab }
public enum ModuleStatus { available, occupied, maintenance }
public enum BookingStatus { confirmed, active, completed }
public enum SensorType { o2, co2, pressure, temperature, humidity, water }
public enum AlertSeverity { info, warning, critical }
public enum AlertStatus { open, acknowledged, resolved }
public enum ExcursionBookingStatus { booked, cancelled, attended }
