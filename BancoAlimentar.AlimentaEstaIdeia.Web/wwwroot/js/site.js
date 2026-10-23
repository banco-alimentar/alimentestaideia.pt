// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
  return new bootstrap.Tooltip(tooltipTriggerEl)
})

document.addEventListener('DOMContentLoaded', function () {
  function focusValidationErrors() {
    var validationSummary = document.getElementById('donation-validation-summary')
    if (validationSummary && validationSummary.querySelector('.validation-summary-errors')) {
      validationSummary.focus()
      return
    }

    var fieldError = document.querySelector('[data-valmsg-for].field-validation-error')
    if (!fieldError) {
      return
    }

    var fieldName = fieldError.getAttribute('data-valmsg-for')
    var invalidField = document.querySelector('[name="' + fieldName + '"]')
    if (invalidField) {
      invalidField.focus()
    }
  }

  focusValidationErrors()

  if (window.jQuery) {
    window.jQuery('#donationForm').on('invalid-form.validate', function () {
      window.setTimeout(function () {
        focusValidationErrors()
      }, 0)
    })
  }

  var customMenuToggle = document.getElementById('menu-toggle')
  var customMenu = document.getElementById('hidden-menu')

  function setCustomMenuOpen(open) {
    if (!customMenuToggle || !customMenu) {
      return
    }

    customMenuToggle.setAttribute('aria-expanded', open.toString())
    customMenu.setAttribute('aria-hidden', (!open).toString())
    customMenu.hidden = !open
  }

  if (customMenuToggle && customMenu) {
    customMenuToggle.addEventListener('click', function () {
      setCustomMenuOpen(customMenuToggle.getAttribute('aria-expanded') !== 'true')
    })
  }

  document.addEventListener('keydown', function (event) {
    if (event.key !== 'Escape') {
      return
    }

    if (customMenuToggle && customMenuToggle.getAttribute('aria-expanded') === 'true') {
      setCustomMenuOpen(false)
      customMenuToggle.focus()
      return
    }

    var openNavigation = document.querySelector('.navbar-collapse.show')
    if (!openNavigation || !window.jQuery) {
      return
    }

    window.jQuery(openNavigation).collapse('hide')
    var navigationToggle = document.querySelector('[aria-controls="' + openNavigation.id + '"]')
    if (navigationToggle) {
      navigationToggle.focus()
    }
  })
})
