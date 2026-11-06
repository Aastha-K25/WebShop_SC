
using System; // Giver adgang til grundlæggende funktioner i C#
using System.Collections.Generic; // Bruges til samlinger som List
using System.Linq; // Bruges til at arbejde med samlinger
using System.Text; // Bruges til at arbejde med tekst
using System.Threading.Tasks; // Bruges til asynkrone opgaver

namespace Obligatoris.opgave.Domain.Models;

// Enum bruges til at samle faste værdier
// Her bestemmer vi hvilke statusser en ordre kan have
// En ordre kan kun have en af disse statusser ad gangen
public enum OrderStatus
{
    // Ordren er oprettet og afventer betaling eller behandling
    Pending,

    // Ordren er betalt
    Paid,

    // Ordren er ved at blive behandlet
    Processing,

    // Ordren er blevet afsendt
    Shipped,

    // Ordren er afsluttet
    Completed,

    // Ordren er blevet annulleret
    Cancelled
}
