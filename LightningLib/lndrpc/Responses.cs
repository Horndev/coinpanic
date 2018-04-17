using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightningLib.lndrpc
{
    public class Node1Policy
    {
        public int time_lock_delta { get; set; }
        public string min_htlc { get; set; }
        public string fee_base_msat { get; set; }
        public string fee_rate_milli_msat { get; set; }
    }

    public class Node2Policy
    {
        public int time_lock_delta { get; set; }
        public string min_htlc { get; set; }
        public string fee_base_msat { get; set; }
        public string fee_rate_milli_msat { get; set; }
    }

    public class GetChanInfoResponse
    {
        public string channel_id { get; set; }
        public string chan_point { get; set; }
        public int last_update { get; set; }
        public string node1_pub { get; set; }
        public string node2_pub { get; set; }
        public string capacity { get; set; }
        public Node1Policy node1_policy { get; set; }
        public Node2Policy node2_policy { get; set; }
    }

    /// <summary>
    /// IP Address information
    /// </summary>
    public class Address
    {
        public string network { get; set; }
        public string addr { get; set; }
    }

    public class Node
    {
        public int last_update { get; set; }
        public string pub_key { get; set; }
        public string alias { get; set; }
        public List<Address> addresses { get; set; }
        public string color { get; set; }
    }

    public class GetNodeInfoResponse
    {
        public Node node { get; set; }
        public int num_channels { get; set; }
        public string total_capacity { get; set; }
    }

    public class Invoice
    {
        public string memo { get; set; }
        public string r_preimage { get; set; }
        public string r_hash { get; set; }
        public string value { get; set; }
        public bool settled { get; set; }
        public string creation_date { get; set; }
        public string settle_date { get; set; }
        public string payment_request { get; set; }
        public string expiry { get; set; }
        public string cltv_expiry { get; set; }
    }

    public class InvoiceEvent
    {
        public Invoice result { get; set; }
    }

    public class GetInfoResponse
    {
        public string identity_pubkey { get; set; }
        public string alias { get; set; }
        public int num_active_channels { get; set; }
        public int num_peers { get; set; }
        public int block_height { get; set; }
        public string block_hash { get; set; }
        public bool synced_to_chain { get; set; }
        public List<string> chains { get; set; }
        public List<string> uris { get; set; }
        public string best_header_timestamp { get; set; }
    }

    /// <summary>
    /// getnetworkinfo
    /// 
    /// Information about the known Lightning Network
    /// </summary>
    public class GetGraphInfoResponse
    {
        public double avg_out_degree { get; set; }
        public int max_out_degree { get; set; }
        public int num_nodes { get; set; }
        public int num_channels { get; set; }
        public string total_network_capacity { get; set; }
        public double avg_channel_size { get; set; }
        public string min_channel_size { get; set; }
        public string max_channel_size { get; set; }
    }

    public class Channel
    {
        public bool active { get; set; }
        public string remote_pubkey { get; set; }
        public string channel_point { get; set; }
        public string chan_id { get; set; }
        public string capacity { get; set; }
        public string remote_balance { get; set; }
        public string commit_fee { get; set; }
        public string commit_weight { get; set; }
        public string fee_per_kw { get; set; }
        public int csv_delay { get; set; }
        public string local_balance { get; set; }
        public string num_updates { get; set; }
        public string total_satoshis_sent { get; set; }
        public string total_satoshis_received { get; set; }
    }

    public class GetChannelsResponse
    {
        public List<Channel> channels { get; set; }
    }

    public class FwdRequest
    {
        public string start_time { get; set; }
        public string end_time { get; set; }
        public Int64 index_offset { get; set; }
        public Int64 num_max_events { get; set; }
    }

    public class ForwardingEvent
    {
        public string timestamp { get; set; }
        public string chan_id_in { get; set; }
        public string chan_id_out { get; set; }
        public string amt_in { get; set; }
        public string amt_out { get; set; }
        public string fee { get; set; }
    }

    public class ForwardingEventsResponse
    {
        public List<ForwardingEvent> forwarding_events { get; set; }
        public int last_offset_index { get; set; }
    }

    public class AddInvoiceResponse
    {
        public string r_hash { get; set; }
        public string payment_request { get; set; }
    }

    public class DecodePaymentResponse
    {
        public string destination { get; set; }
        public string payment_hash { get; set; }
        public string num_satoshis { get; set; }
        public string timestamp { get; set; }
        public string expiry { get; set; }
        public string cltv_expiry { get; set; }
    }

    public class Hop
    {
        public string chan_id { get; set; }
        public string chan_capacity { get; set; }
        public string amt_to_forward { get; set; }
        public int expiry { get; set; }
    }

    public class PaymentRoute
    {
        public int total_time_lock { get; set; }
        public string total_amt { get; set; }
        public string total_fees { get; set; }
        public List<Hop> hops { get; set; }
    }

    public class SendPaymentResponse
    {
        public string payment_error { get; set; }
        public string payment_preimage { get; set; }
        public PaymentRoute payment_route { get; set; }
    }
}
