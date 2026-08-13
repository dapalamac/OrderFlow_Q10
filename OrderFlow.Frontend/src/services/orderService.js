const API_URL = "http://localhost:5216";

export async function createOrder(order) {
    const response = await fetch(`${API_URL}/orders`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(order)
    });

    const data = await response.json();

    console.log("Response from createOrder:", data);

    if (!response.ok) {
        throw new Error(
            data.message || "No se pudo crear el pedido."
        );
    }

    return data;
}

export async function getOrders() {
    const response = await fetch(`${API_URL}/orders`);

    if (!response.ok) {
        throw new Error("No se pudieron obtener los pedidos.");
    }

    return await response.json();
}

export async function getOrderById(id) {
    const response = await fetch(`${API_URL}/orders/${id}`);

    if (!response.ok) {
        throw new Error("No se pudo obtener el pedido.");
    }

    return await response.json();
}