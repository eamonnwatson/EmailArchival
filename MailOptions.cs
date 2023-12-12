namespace EmailArchival;
internal class MailOptions
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool UseSSL { get; set; }
}
