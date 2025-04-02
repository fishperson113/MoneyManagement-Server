using API.Models.DTOs;
using API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WalletsController : ControllerBase
    {
        private readonly IWalletRepository _walletRepository;
        private readonly ILogger<WalletsController> _logger;

        public WalletsController(IWalletRepository walletRepository, ILogger<WalletsController> logger)
        {
            _walletRepository = walletRepository;
            _logger = logger;
        }

        // GET: api/<WalletsController>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WalletDTO>>> GetAllWallets()
        {
            try
            {
                var wallets = await _walletRepository.GetAllWalletsAsync();
                return Ok(wallets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all wallets");
                return StatusCode(500, "An error occurred while retrieving wallets");
            }
        }

        // GET api/<WalletsController>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<WalletDTO>> GetWalletById(Guid id)
        {
            try
            {
                var wallet = await _walletRepository.GetWalletByIdAsync(id);

                if (wallet == null)
                {
                    return NotFound($"Wallet with ID {id} not found");
                }

                return Ok(wallet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving wallet with ID {WalletId}", id);
                return StatusCode(500, "An error occurred while retrieving the wallet");
            }
        }

        // POST api/<WalletsController>
        [HttpPost]
        public async Task<ActionResult<WalletDTO>> CreateWallet([FromBody] CreateWalletDTO createWalletDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdWallet = await _walletRepository.CreateWalletAsync(createWalletDTO);
                return CreatedAtAction(nameof(GetWalletById), new { id = createdWallet.WalletID }, createdWallet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating a wallet");
                return StatusCode(500, "An error occurred while creating the wallet");
            }
        }

        // PUT api/<WalletsController>/5
        [HttpPut]
        public async Task<ActionResult<WalletDTO>> UpdateWallet([FromBody] UpdateWalletDTO updateWalletDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var updatedWallet = await _walletRepository.UpdateWalletAsync(updateWalletDTO);
                return Ok(updatedWallet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating wallet with ID {WalletId}", updateWalletDTO.WalletID);
                return StatusCode(500, "An error occurred while updating the wallet");
            }
        }

        // DELETE api/<WalletsController>/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteWallet(Guid id)
        {
            try
            {
                await _walletRepository.DeleteWalletByIdAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting wallet with ID {WalletId}", id);
                return StatusCode(500, "An error occurred while deleting the wallet");
            }
        }
    }
}
