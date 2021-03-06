﻿using AutoMapper;
using Pds.Audit.Api.Client.Enumerations;
using Pds.Audit.Api.Client.Interfaces;
using Pds.Contracts.Data.Common.Enums;
using Pds.Contracts.Data.Common.Responses;
using Pds.Contracts.Data.Repository.Interfaces;
using Pds.Contracts.Data.Services.Interfaces;
using Pds.Contracts.Data.Services.Models;
using Pds.Contracts.Data.Services.Responses;
using Pds.Core.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pds.Contracts.Data.Services.Implementations
{
    /// <inheritdoc/>
    public class ContractService : IContractService
    {
        private const string _appName = "Pds.Contracts.Data.Api";

        private readonly IContractRepository _repository;

        private readonly IMapper _mapper;

        private readonly IUriService _uriService;

        private readonly ILoggerAdapter<ContractService> _logger;

        private readonly IAuditService _auditService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContractService"/> class.
        /// </summary>
        /// <param name="repository">Contracts repository.</param>
        /// <param name="mapper">Automapper instance.</param>
        /// <param name="uriService">The uri service.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="auditService">The audit service used for auditing.</param>
        public ContractService(IContractRepository repository, IMapper mapper, IUriService uriService, ILoggerAdapter<ContractService> logger, IAuditService auditService)
        {
            _repository = repository;
            _mapper = mapper;
            _uriService = uriService;
            _logger = logger;
            _auditService = auditService;
        }

        /// <inheritdoc/>
        public async Task<Models.Contract> GetAsync(int id)
        {
            var contract = await _repository.GetAsync(id).ConfigureAwait(false);
            return _mapper.Map<Models.Contract>(contract);
        }

        /// <inheritdoc/>
        public async Task<IList<Models.Contract>> GetByContractNumberAsync(string contractNumber)
        {
            var contracts = await _repository.GetByContractNumberAsync(contractNumber);
            return _mapper.Map<IList<Models.Contract>>(contracts);
        }

        /// <inheritdoc/>
        public async Task<Models.Contract> GetByContractNumberAndVersionAsync(string contractNumber, int version)
        {
            var contract = await _repository.GetByContractNumberAndVersionAsync(contractNumber, version).ConfigureAwait(false);
            return _mapper.Map<Models.Contract>(contract);
        }

        /// <inheritdoc/>
        public async Task<ContractReminderResponse<IEnumerable<ContractReminderItem>>> GetContractRemindersAsync(int reminderInterval, int pageNumber, int pageSize, ContractSortOptions sort, SortDirection order, string templatedQueryString)
        {
            DateTime currentDateTimeMinusNumberOfDays = DateTime.UtcNow.Date.AddDays(-reminderInterval).AddHours(23).AddMinutes(59);

            _logger.LogInformation($"Get contract reminder by reminder interval : {reminderInterval} and cut off datetime {currentDateTimeMinusNumberOfDays}. - Current utc: {DateTime.UtcNow}");

            var contracts = await _repository.GetContractRemindersAsync(currentDateTimeMinusNumberOfDays, pageNumber, pageSize, sort, order);

            var metadata = new Metadata
            {
                TotalCount = contracts.TotalCount,
                PageSize = contracts.PageSize,
                CurrentPage = contracts.CurrentPage,
                TotalPages = contracts.TotalPages,
                HasNextPage = contracts.HasNextPage,
                HasPreviousPage = contracts.HasPreviousPage,
                NextPageUrl = contracts.HasNextPage ? _uriService.GetUri(SetPageValue(templatedQueryString, pageNumber + 1)).ToString() : string.Empty,
                PreviousPageUrl = contracts.HasPreviousPage ? _uriService.GetUri(SetPageValue(templatedQueryString, pageNumber - 1)).ToString() : string.Empty
            };

            var contractReminders = _mapper.Map<IEnumerable<ContractReminderItem>>(contracts.Items);

            var apiResponse = new ContractReminderResponse<IEnumerable<ContractReminderItem>>(contractReminders)
            {
                Paging = metadata
            };

            return apiResponse;
        }

        /// <inheritdoc/>
        public async Task<Contract> UpdateLastEmailReminderSentAndLastUpdatedAtAsync(UpdateLastEmailReminderSentRequest request)
        {
            var contract = await _repository.UpdateLastEmailReminderSentAndLastUpdatedAtAsync(request.Id);
            return _mapper.Map<Models.Contract>(contract);
        }

        /// <inheritdoc/>
        public async Task<UpdatedContractStatusResponse> UpdateContractConfirmApprovalAsync(UpdateConfirmApprovalRequest request)
        {
            _logger.LogInformation($"[UpdateContractConfirmApprovalAsync] called with contract number: {request.ContractNumber}, contract Id: {request.Id} ");
            ContractStatus requiredContractStatus = ContractStatus.ApprovedWaitingConfirmation;
            ContractStatus newContractStatus = ContractStatus.Approved;
            var updatedContractStatusResponse = await _repository.UpdateContractStatusAsync(request.Id, requiredContractStatus, newContractStatus);

            string message = $"Contract [{updatedContractStatusResponse.ContractNumber}] Version number [{updatedContractStatusResponse.ContractVersion}] with Id [{updatedContractStatusResponse.Id}] has been {updatedContractStatusResponse.NewStatus}. Additional Information Details: ContractId is: {updatedContractStatusResponse.Id}. Contract Status Before was {updatedContractStatusResponse.Status} . Contract Status After is {updatedContractStatusResponse.NewStatus}";
            try
            {
                await _auditService.AuditAsync(new Audit.Api.Client.Models.Audit()
                {
                    Action = ActionType.ContractConfirmApproval,
                    Severity = SeverityLevel.Information,
                    Ukprn = updatedContractStatusResponse.Ukprn,
                    Message = message,
                    User = $"[{_appName}]"
                });

                _logger.LogInformation($"[UpdateContractConfirmApproval] Audit success for the message: {message}");
            }
            catch (Exception e)
            {
                //Silent log with the message and error details.
                _logger.LogError($"[UpdateContractConfirmApproval] Audit log failed for the contract number: {updatedContractStatusResponse.ContractNumber}, contract Id: {updatedContractStatusResponse.Id}. Message: {message}. The Error: {e.Message}");
            }

            return updatedContractStatusResponse;
        }

        /// <summary>
        /// Replace the templated page with page number.
        /// </summary>
        /// <param name="templatedQueryString">templated query string.</param>
        /// <param name="pageValue">page number.</param>
        /// <returns>Returns formatted url.</returns>
        private string SetPageValue(string templatedQueryString, int pageValue)
        {
            return templatedQueryString.Replace("{page}", pageValue.ToString());
        }
    }
}