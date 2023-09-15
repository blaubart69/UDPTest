use std::error::Error;
use std::{env,io};

use tokio::net::UdpSocket;

async fn receive(socket: UdpSocket) -> Result<(), io::Error> {

	let mut buf : Vec<u8> = vec![0; 1024];

	loop {
		let (datalen, from) = socket.recv_from(&mut buf[..]).await?;
		println!("received {} bytes from {}", datalen, from);
	}
}


#[tokio::main]
async fn main() -> Result<(), Box<dyn Error>> {
    let addr = env::args()
        .nth(1)
        .unwrap_or_else(|| "0.0.0.0:8080".to_string());

    let socket = UdpSocket::bind(&addr).await?;
    println!("Listening on: {}", socket.local_addr()?);

    receive(socket).await?;

    Ok(())
}
