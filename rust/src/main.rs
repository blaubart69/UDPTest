use std::error::Error;
use std::net::{SocketAddr, IpAddr};
use std::sync::Arc;
use std::{env,io};

use tokio::net::UdpSocket;


//
// we have to set ReUse BEFORE we do the bind()
//  otherwise it dosn't work
//
async fn receive(addr: SocketAddr) -> Result<(), io::Error> {

	let mut buf : Vec<u8> = vec![0; 1024];

    let socket = tokio::net::UdpSocket::from_std(
    {
        // use socket2 library to set the socketoptions without the use for unsafe
        let s2 = socket2::Socket::new(socket2::Domain::for_address(addr), socket2::Type::DGRAM, None)?;
        s2.set_reuse_address(true)?;
        s2.set_reuse_port(true)?;
        s2.bind(&addr.into())?;     // bind() have to be AFTER REUSE!!!
        s2.into()
    })?;

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
    let str_ip  = env::args()
        .nth(1)
        .unwrap_or_else(|| "0.0.0.0".to_string());

    let ip = str_ip.parse()?;

    let receive_futues = [0;3].iter()
    .map( |_|
    {
        let sock_addr = std::net::SocketAddr::new(ip, 34000);
        println!("spawning recv new...");
        tokio::spawn( receive(sock_addr.clone()))
    }).collect::<Vec<_>>();

    println!("started {} receives. waiting...", receive_futues.len());
    futures::future::try_join_all(receive_futues).await?;

    Ok(())
}
