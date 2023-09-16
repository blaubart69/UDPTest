use std::error::Error;
use std::net::SocketAddr;
use std::sync::Arc;
use std::{env,io};

use tokio::net::UdpSocket;

async fn receive(addr: Arc<String>) -> Result<(), io::Error> {

	let mut buf : Vec<u8> = vec![0; 1024];

    let s2 = socket2::Socket::new(socket2::Domain::IPV4, socket2::Type::DGRAM, None)?;
    s2.set_reuse_address(true)?;
    s2.set_reuse_port(true)?;
    
    let address: SocketAddr = addr.parse().unwrap();
    s2.bind(&address.into())?;

    //let socket = UdpSocket::bind(&addr.as_str()).await?;
    let socket = tokio::net::UdpSocket::from_std(s2.into())?;
    
    println!("bound to: {}", socket.local_addr()?);

	loop {
        let recv_from_future = socket.recv_from(&mut buf[..]);
        println!("recv_from...");
		let (datalen, from) = recv_from_future.await?;
		println!("received {} bytes from {}", datalen, from);
	}
}

#[tokio::main]
async fn main() -> Result<(), Box<dyn Error>> {
    let addr  = env::args()
        .nth(1)
        .unwrap_or_else(|| "0.0.0.0:8080".to_string());

    let arc_addr = Arc::new(addr);

    let mut receive_futues = Vec::new();
    for _ in 0..3  
    {
        println!("spawning recv...");
        receive_futues.push( 
            tokio::spawn(
                receive(arc_addr.clone() )));
    }

    println!("waiting for receives...");
    futures::future::try_join_all(receive_futues).await?;

    Ok(())
}
