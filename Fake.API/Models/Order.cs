using Stateless;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Fake.API.Models
{
    public enum OrderStateEnum
    {
        Pending, //訂單已生成
        Processing, //支付處理中
        Completed, //交易成功
        Declined, //交易失敗
        Cancelled, //訂單取消
        Refund //已退款
    }

    public enum OrderStateTriggerEnum
    {
        PlaceOrder, //支付
        Approve, //收款成功
        Reject, //收款失敗
        Cancel, //取消
        Return //退貨
    }

    public class Order
    {
        public Order()
        {
            StateMachineInit();
        }

        [Key]
        public Guid Id { get; set; }

        public string UserId { get; set; }

        public ApplicationUser User { get; set; }

        public ICollection<LineItem> OrderItems { get; set; }

        public OrderStateEnum State { get; set; }

        public DateTime CreateDateUTC { get; set; }

        public string TransactionMetadata { get; set; }

        private StateMachine<OrderStateEnum, OrderStateTriggerEnum> _machine { get; set; }
    
        public void PaymentProcessing()
        {
            _machine.Fire(OrderStateTriggerEnum.PlaceOrder);
        }

        public void PaymentApprove()
        {
            _machine.Fire(OrderStateTriggerEnum.Approve);
        }

        public void PaymentReject()
        {
            _machine.Fire(OrderStateTriggerEnum.Reject);
        }

        private void StateMachineInit()
        {
            _machine = new StateMachine<OrderStateEnum, OrderStateTriggerEnum>(
                () => State, s => State = s);

            //訂單已生成
            _machine.Configure(OrderStateEnum.Pending)
                //支付 -> 支付處理中
                .Permit(OrderStateTriggerEnum.PlaceOrder, OrderStateEnum.Processing)
                //取消
                .Permit(OrderStateTriggerEnum.Cancel, OrderStateEnum.Cancelled);

            //支付處理中
            _machine.Configure(OrderStateEnum.Processing)
                //收款成功 -> 交易成功
                .Permit(OrderStateTriggerEnum.Approve, OrderStateEnum.Completed)
                //收款失敗 -> 交易失敗
                .Permit(OrderStateTriggerEnum.Reject, OrderStateEnum.Declined);

            //交易失敗
            _machine.Configure(OrderStateEnum.Declined)
                //支付 -> 支付處理中
                .Permit(OrderStateTriggerEnum.PlaceOrder, OrderStateEnum.Processing);

            //交易成功
            _machine.Configure(OrderStateEnum.Completed)
                //退貨 -> 已退款
                .Permit(OrderStateTriggerEnum.Return, OrderStateEnum.Refund);
        }
    }
}
