import React from "react";

interface FooterProps { note?: React.ReactNode; }

const Footer: React.FC<FooterProps> = ({ note }) => {
  return <footer className="page-footer">{note || <>© {new Date().getFullYear()} SmartAuth</>}</footer>;
};

export default Footer;
