import { useEffect, useState } from "react";
import { getOrders } from "../services/orderService";

function OrderList({ refreshTrigger }) {
    const [orders, setOrders] = useState([]);
    const [error, setError] = useState("");
    const [loading, setLoading] = useState(true);

    async function loadOrders() {
        try {
            setError("");

            const data = await getOrders();

            setOrders(data);
        } catch (error) {
            setError(error.message);
        } finally {
            setLoading(false);
        }
    }

    useEffect(() => {
        loadOrders();

        const interval = setInterval(() => {
            loadOrders();
        }, 3000);

        return () => {
            clearInterval(interval);
        };
    }, [refreshTrigger]);

    return (
        <section>
            <h2>Pedidos</h2>

            {loading && <p>Cargando pedidos...</p>}

            {error && (
                <p>
                    {error}
                </p>
            )}

            {!loading && !error && orders.length === 0 && (
                <p>No hay pedidos registrados.</p>
            )}

            {orders.length > 0 && (
                <table>
                    <thead>
                        <tr>
                            <th>Cliente</th>
                            <th>SKU</th>
                            <th>Cantidad</th>
                            <th>Estado</th>
                            <th>Fecha</th>
                        </tr>
                    </thead>

                    <tbody>
                        {orders.map((order) => (
                            <tr key={order.id}>
                                <td>{order.customerName}</td>
                                <td>{order.sku}</td>
                                <td>{order.quantity}</td>
                                <td>{getStatusText(order.status)}</td>
                                <td>
                                    {new Date(`${order.createdAt}Z`).toLocaleString("es-CO", {
                                        timeZone: "America/Bogota",
                                        dateStyle: "short",
                                        timeStyle: "medium"
                                    })}
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            )}
        </section>
    );
}

function getStatusText(status) {
    switch (status) {
        case 0:
            return "Pending";

        case 1:
            return "Confirmed";

        case 2:
            return "Rejected";

        default:
            return "Unknown";
    }
}

export default OrderList;